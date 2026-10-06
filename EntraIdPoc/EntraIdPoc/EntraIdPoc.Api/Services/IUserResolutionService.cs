using System.Security.Claims;
using EntraIdPoc.Api.Models;

namespace EntraIdPoc.Api.Services;

/// <summary>
/// Core service that bridges Microsoft Entra ID claims to your database users.
/// Implements auto-provisioning: creates DB user on first login.
/// </summary>
public interface IUserResolutionService
{
    /// <summary>
    /// Resolve or auto-provision user from Entra claims.
    /// Called on every authenticated request.
    /// </summary>
    Task<User> ResolveUserAsync(ClaimsPrincipal entraUser, CancellationToken ct = default);

    /// <summary>
    /// Get full profile including app-specific data.
    /// </summary>
    Task<UserProfileResponse> GetProfileAsync(Guid dbUserId, CancellationToken ct = default);

    /// <summary>
    /// Update app-specific fields (not Entra identity fields).
    /// </summary>
    Task<User> UpdateProfileAsync(Guid dbUserId, UpdateProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Admin-only: Update any user's role.
    /// </summary>
    Task<User> UpdateUserRoleAsync(Guid targetUserId, string newRole, CancellationToken ct = default);

    /// <summary>
    /// Admin-only: List all users.
    /// </summary>
    Task<List<AdminUserResponse>> GetAllUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Admin-only: Toggle user active status.
    /// </summary>
    Task<User> ToggleUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);

    /// <summary>
    /// Get dashboard statistics.
    /// </summary>
    Task<DashboardStatsResponse> GetDashboardStatsAsync(Guid currentUserId, CancellationToken ct = default);
}
