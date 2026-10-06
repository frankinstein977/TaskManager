using EntraIdPoc.Api.Models;
using EntraIdPoc.Api.Data;

namespace EntraIdPoc.Api.Services;

public interface IAuditService
{
    Task LogAsync(Guid? userId, string action, string? details = null, CancellationToken ct = default);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContext;

    public AuditService(AppDbContext db, IHttpContextAccessor httpContext)
    {
        _db = db;
        _httpContext = httpContext;
    }

    public async Task LogAsync(Guid? userId, string action, string? details = null, CancellationToken ct = default)
    {
        var ip = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            IpAddress = ip
        });

        await _db.SaveChangesAsync(ct);
    }
}
