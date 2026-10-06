using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EntraIdPoc.Api.Data;
using EntraIdPoc.Api.Models;

namespace EntraIdPoc.Api.Services;

public class UserResolutionService : IUserResolutionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserResolutionService> _logger;
    private readonly IAuditService _audit;

    public UserResolutionService(AppDbContext db, ILogger<UserResolutionService> logger, IAuditService audit)
    {
        _db = db;
        _logger = logger;
        _audit = audit;
    }

    public async Task<User> ResolveUserAsync(ClaimsPrincipal entraUser, CancellationToken ct = default)
    {
        var oid = entraUser.FindFirst("oid")?.Value 
            ?? entraUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Entra OID claim not found in token");

        var tid = entraUser.FindFirst("tid")?.Value;
        var email = entraUser.FindFirst("preferred_username")?.Value 
            ?? entraUser.FindFirst(ClaimTypes.Email)?.Value;
        var displayName = entraUser.FindFirst("name")?.Value;
        var givenName = entraUser.FindFirst(ClaimTypes.GivenName)?.Value;
        var surname = entraUser.FindFirst(ClaimTypes.Surname)?.Value;

        var isGuest = entraUser.FindFirst("idtyp")?.Value == "guest"
            || (email?.Contains("#EXT#") == true)
            || (entraUser.Identity?.AuthenticationType?.Contains("Guest") ?? false);

        // 1. LINQ query for user lookup
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EntraOid == oid, ct);

        if (user != null)
        {
            // 2. Raw SQL for efficient update (no change tracker overhead)
            await _db.Database.ExecuteSqlAsync($"""
                UPDATE users 
                SET last_login_at = NOW(),
                    email = COALESCE({email}, email),
                    display_name = COALESCE({displayName}, display_name),
                    given_name = COALESCE({givenName}, given_name),
                    surname = COALESCE({surname}, surname),
                    updated_at = NOW()
                WHERE id = {user.Id}
            """, ct);

            user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id, ct);

            _logger.LogInformation("User {Email} logged in. Role: {Role}", email, user.AppRole);
            await _audit.LogAsync(user.Id, "LOGIN", 
                $"{{"source":"Entra","isGuest":{isGuest.ToString().ToLower()}}}", ct);

            return user;
        }

        // 3. Auto-provision new user
        var initialRole = DeriveRoleFromEntra(entraUser, isGuest);
        var newId = Guid.NewGuid();

        await _db.Database.ExecuteSqlAsync($"""
            INSERT INTO users (id, entra_oid, entra_tid, email, display_name, given_name, surname, 
                             app_role, is_guest, is_active, last_login_at, created_at, updated_at)
            VALUES ({newId}, {oid}, {tid}, {email}, {displayName}, {givenName}, {surname},
                    {initialRole}, {isGuest}, {true}, {DateTime.UtcNow}, {DateTime.UtcNow}, {DateTime.UtcNow})
        """, ct);

        user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == newId, ct);

        _logger.LogInformation("Auto-provisioned: {Email} with role {Role}", email, initialRole);
        await _audit.LogAsync(user.Id, "USER_PROVISIONED", 
            $"{{"email":"{email}","role":"{initialRole}"}}", ct);

        return user;
    }

    private static string DeriveRoleFromEntra(ClaimsPrincipal entraUser, bool isGuest)
    {
        var entraRoles = entraUser.FindAll("roles").Select(c => c.Value).ToList();
        if (entraRoles.Contains("App.Admin")) return AppRoles.Admin;
        if (entraRoles.Contains("App.Editor")) return AppRoles.Editor;
        if (entraRoles.Contains("App.User")) return AppRoles.User;
        if (isGuest) return AppRoles.Guest;
        return AppRoles.User;
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid dbUserId, CancellationToken ct = default)
    {
        // LINQ projection
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == dbUserId)
            .Select(u => new UserProfileResponse(
                u.Id, u.EntraOid, u.Email, u.DisplayName, u.AppRole,
                u.IsGuest, u.IsActive, u.Department, u.Preferences,
                u.LastLoginAt, u.CreatedAt))
            .FirstAsync(ct);
    }

    public async Task<User> UpdateProfileAsync(Guid dbUserId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlAsync($"""
            UPDATE users 
            SET department = COALESCE({request.Department}, department),
                preferences = COALESCE({request.Preferences}, preferences),
                updated_at = NOW()
            WHERE id = {dbUserId}
        """, ct);

        var user = await _db.Users.FindAsync(new object[] { dbUserId }, ct)
            ?? throw new InvalidOperationException("User not found");

        await _audit.LogAsync(user.Id, "PROFILE_UPDATED", null, ct);
        return user;
    }

    public async Task<User> UpdateUserRoleAsync(Guid targetUserId, string newRole, CancellationToken ct = default)
    {
        if (!AppRoles.All.Contains(newRole))
            throw new ArgumentException($"Invalid role: {newRole}");

        var currentRole = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == targetUserId)
            .Select(u => u.AppRole)
            .FirstOrDefaultAsync(ct);

        if (currentRole == null)
            throw new InvalidOperationException("User not found");

        await _db.Database.ExecuteSqlAsync($"""
            UPDATE users SET app_role = {newRole}, updated_at = NOW()
            WHERE id = {targetUserId}
        """, ct);

        var user = await _db.Users.FindAsync(new object[] { targetUserId }, ct)
            ?? throw new InvalidOperationException("User not found");

        await _audit.LogAsync(user.Id, "ROLE_CHANGED", 
            $"{{"old":"{currentRole}","new":"{newRole}"}}", ct);

        return user;
    }

    public async Task<List<AdminUserResponse>> GetAllUsersAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserResponse(
                u.Id, u.EntraOid, u.Email, u.DisplayName, u.AppRole,
                u.IsGuest, u.IsActive, u.LastLoginAt, u.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<User> ToggleUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var rows = await _db.Database.ExecuteSqlAsync($"""
            UPDATE users SET is_active = {isActive}, updated_at = NOW()
            WHERE id = {userId}
        """, ct);

        if (rows == 0) throw new InvalidOperationException("User not found");

        var user = await _db.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new InvalidOperationException("User not found");

        await _audit.LogAsync(user.Id, isActive ? "USER_ACTIVATED" : "USER_DEACTIVATED", null, ct);
        return user;
    }

    public async Task<DashboardStatsResponse> GetDashboardStatsAsync(Guid currentUserId, CancellationToken ct = default)
    {
        var totalUsers = await _db.Users.CountAsync(ct);
        var activeUsers = await _db.Users.CountAsync(u => u.IsActive, ct);
        var guestUsers = await _db.Users.CountAsync(u => u.IsGuest, ct);
        var totalProjects = await _db.Projects.CountAsync(ct);
        var myProjects = await _db.Projects.CountAsync(p => p.OwnerId == currentUserId, ct);

        return new DashboardStatsResponse(totalUsers, activeUsers, guestUsers, totalProjects, myProjects);
    }
}
