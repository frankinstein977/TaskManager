namespace EntraIdPoc.Api.Models;

/// <summary>
/// Application user entity — linked to Microsoft Entra ID via EntraOid.
/// This is YOUR database's source of truth for application-level user data.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Immutable Microsoft Entra Object ID (oid claim from JWT).
    /// This is the bridge between Entra ID and your database.
    /// </summary>
    public string EntraOid { get; set; } = string.Empty;

    /// <summary>
    /// Microsoft Tenant ID (tid claim). Useful for multi-tenant scenarios.
    /// </summary>
    public string? EntraTid { get; set; }

    /// <summary>
    /// Email from Entra ID (preferred_username claim).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Display name from Entra ID (name claim).
    /// </summary>
    public string? DisplayName { get; set; }

    public string? GivenName { get; set; }
    public string? Surname { get; set; }

    /// <summary>
    /// Application-specific role. Derived from Entra app roles on first login,
    /// but can be overridden by admin in your app.
    /// Values: "admin", "editor", "user", "guest"
    /// </summary>
    public string AppRole { get; set; } = "user";

    /// <summary>
    /// Department or business unit — app-specific data not in Entra.
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// JSON preferences: theme, notifications, language, etc.
    /// </summary>
    public string Preferences { get; set; } = "{}";

    /// <summary>
    /// Is this user active? Admins can disable accounts without deleting data.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Is this an external/guest user? Derived from Entra user type.
    /// Guests typically get read-only access by default.
    /// </summary>
    public bool IsGuest { get; set; } = false;

    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

/// <summary>
/// Application role enum for type safety.
/// </summary>
public static class AppRoles
{
    public const string Admin = "admin";
    public const string Editor = "editor";
    public const string User = "user";
    public const string Guest = "guest";

    public static readonly string[] All = { Admin, Editor, User, Guest };

    /// <summary>
    /// Returns true if the role has write access (not read-only).
    /// </summary>
    public static bool CanWrite(string role) => role is Admin or Editor;

    public static bool CanAdmin(string role) => role == Admin;
}
