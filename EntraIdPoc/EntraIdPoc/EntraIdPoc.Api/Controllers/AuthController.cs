using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EntraIdPoc.Api.Middleware;
using EntraIdPoc.Api.Services;

namespace EntraIdPoc.Api.Controllers;

/// <summary>
/// Authentication endpoints.
/// Handles login/logout via Microsoft Entra ID.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initiate login with Microsoft Entra ID.
    /// Redirects to Microsoft login page.
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login(string? redirectUri = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUri,
            Items = { { "scheme", OpenIdConnectDefaults.AuthenticationScheme } }
        };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Logout and clear session.
    /// </summary>
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/"
        };

        return SignOut(properties, 
            OpenIdConnectDefaults.AuthenticationScheme, 
            "Cookies");
    }

    /// <summary>
    /// Check current authentication status.
    /// Returns user info if authenticated, 401 if not.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
    public async Task<IActionResult> Me(
        [FromServices] CurrentUserContext currentUser,
        [FromServices] IUserResolutionService userService)
    {
        if (!currentUser.IsAuthorized)
            return Unauthorized(new { error = "User not resolved or inactive" });

        var profile = await userService.GetProfileAsync(currentUser.DbId);
        return Ok(profile);
    }
}
