namespace EntraIdPoc.Api.Models;

/// <summary>
/// Example business entity owned by a user.
/// Demonstrates how all app data links to users.Id (not entra_oid).
/// </summary>
public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK to your local users table (NOT entra_oid).
    /// This keeps your app decoupled from the identity provider.
    /// </summary>
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "active"; // active, archived, completed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
