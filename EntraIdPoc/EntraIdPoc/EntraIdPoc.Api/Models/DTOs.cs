namespace EntraIdPoc.Api.Models;

// ==================== REQUEST DTOs ====================

public record CreateProjectRequest(string Title, string? Description);
public record UpdateProjectRequest(string Title, string? Description, string Status);
public record UpdateProfileRequest(string? Department, string? Preferences);
public record UpdateUserRoleRequest(string AppRole);

// ==================== RESPONSE DTOs ====================

/// <summary>
/// Merged user profile: Entra identity + app data.
/// </summary>
public record UserProfileResponse(
    Guid DbId,
    string EntraOid,
    string? Email,
    string? DisplayName,
    string AppRole,
    bool IsGuest,
    bool IsActive,
    string? Department,
    string Preferences,
    DateTime? LastLoginAt,
    DateTime MemberSince
);

public record ProjectResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record AdminUserResponse(
    Guid Id,
    string EntraOid,
    string? Email,
    string? DisplayName,
    string AppRole,
    bool IsGuest,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt
);

public record DashboardStatsResponse(
    int TotalUsers,
    int ActiveUsers,
    int GuestUsers,
    int TotalProjects,
    int MyProjects
);
