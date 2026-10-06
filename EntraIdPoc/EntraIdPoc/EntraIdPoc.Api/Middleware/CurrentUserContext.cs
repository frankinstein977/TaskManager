using System.Security.Claims;

namespace EntraIdPoc.Api.Middleware;

/// <summary>
/// Holds the resolved database user context for the current HTTP request.
/// Injected into controllers so they don't need to re-resolve the user.
/// </summary>
public class CurrentUserContext
{
    public Guid DbId { get; set; }
    public string EntraOid { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string AppRole { get; set; } = "user";
    public bool IsAdmin => AppRole == Models.AppRoles.Admin;
    public bool CanWrite => Models.AppRoles.CanWrite(AppRole);
    public bool IsGuest { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// True if the user is authenticated AND active.
    /// </summary>
    public bool IsAuthorized => !string.IsNullOrEmpty(EntraOid) && IsActive;
}

/// <summary>
/// Middleware that runs after authentication to resolve the DB user
/// and attach the CurrentUserContext to the request.
/// </summary>
public class UserResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserResolutionMiddleware> _logger;

    public UserResolutionMiddleware(RequestDelegate next, ILogger<UserResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        CurrentUserContext currentUser,
        Services.IUserResolutionService userService)
    {
        // Only process if user is authenticated by Entra
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                // Resolve or auto-provision user in DB
                var dbUser = await userService.ResolveUserAsync(context.User, context.RequestAborted);

                // Populate the request-scoped context
                currentUser.DbId = dbUser.Id;
                currentUser.EntraOid = dbUser.EntraOid;
                currentUser.Email = dbUser.Email;
                currentUser.AppRole = dbUser.AppRole;
                currentUser.IsGuest = dbUser.IsGuest;
                currentUser.IsActive = dbUser.IsActive;

                // Make it available as a claim for downstream authorization
                var identity = (ClaimsIdentity)context.User.Identity;
                identity.AddClaim(new Claim("app_role", dbUser.AppRole));
                identity.AddClaim(new Claim("db_user_id", dbUser.Id.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve user from Entra claims");
                // Don't fail the request — let authorization policies handle it
            }
        }

        await _next(context);
    }
}
