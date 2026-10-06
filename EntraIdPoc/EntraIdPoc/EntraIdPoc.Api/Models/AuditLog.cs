namespace EntraIdPoc.Api.Models;

/// <summary>
/// Audit trail for security and compliance.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty; // e.g., "LOGIN", "PROJECT_CREATED"
    public string? Details { get; set; } // JSON string
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
