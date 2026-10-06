using Microsoft.AspNetCore.Authorization;

namespace EntraIdPoc.Api.Middleware;

/// <summary>
/// Custom authorization requirements and handlers for role-based access.
/// </summary>
public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string EditorOrAdmin = "EditorOrAdmin";
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string NotGuest = "NotGuest"; // Requires write access

    /// <summary>
    /// Configure all authorization policies.
    /// </summary>
    public static void Configure(AuthorizationOptions options)
    {
        // Admin: full access to everything
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireClaim("app_role", "admin"));

        // Editor or Admin: can create/edit content
        options.AddPolicy(EditorOrAdmin, policy =>
            policy.RequireClaim("app_role", "admin", "editor"));

        // Any authenticated user (including guests)
        options.AddPolicy(AuthenticatedUser, policy =>
            policy.RequireAuthenticatedUser());

        // Not Guest: requires write access (admin, editor, or user)
        options.AddPolicy(NotGuest, policy =>
            policy.RequireClaim("app_role", "admin", "editor", "user"));
    }
}
