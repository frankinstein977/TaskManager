using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EntraIdPoc.Api.Middleware;
using EntraIdPoc.Api.Services;

namespace EntraIdPoc.Api.Controllers;

/// <summary>
/// Admin-only endpoints for user management and dashboard.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminController : ControllerBase
{
    private readonly IUserResolutionService _userService;
    private readonly CurrentUserContext _currentUser;

    public AdminController(IUserResolutionService userService, CurrentUserContext currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all users (admin only).
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Update a user's role (admin only).
    /// </summary>
    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Prevent self-demotion
        if (id == _currentUser.DbId && request.AppRole != "admin")
            return BadRequest(new { error = "Cannot remove your own admin role" });

        var user = await _userService.UpdateUserRoleAsync(id, request.AppRole);
        return Ok(new { message = $"User role updated to {user.AppRole}" });
    }

    /// <summary>
    /// Activate/deactivate a user (admin only).
    /// </summary>
    [HttpPut("users/{id:guid}/active")]
    public async Task<IActionResult> ToggleUserActive(Guid id, [FromQuery] bool active)
    {
        // Prevent self-deactivation
        if (id == _currentUser.DbId && !active)
            return BadRequest(new { error = "Cannot deactivate yourself" });

        var user = await _userService.ToggleUserActiveAsync(id, active);
        return Ok(new { message = $"User {(active ? "activated" : "deactivated")}" });
    }

    /// <summary>
    /// Get dashboard statistics.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var stats = await _userService.GetDashboardStatsAsync(_currentUser.DbId);
        return Ok(stats);
    }
}
