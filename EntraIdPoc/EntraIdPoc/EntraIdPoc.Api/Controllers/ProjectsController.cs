using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EntraIdPoc.Api.Middleware;
using EntraIdPoc.Api.Models;
using EntraIdPoc.Api.Services;

namespace EntraIdPoc.Api.Controllers;

/// <summary>
/// Projects API — demonstrates CRUD with role-based authorization.
/// - Guests: read-only (GET)
/// - Users/Editors: full CRUD on own projects
/// - Admins: full CRUD on all projects
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectService projectService,
        CurrentUserContext currentUser,
        ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get my projects (or all projects if admin).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        if (_currentUser.IsAdmin)
        {
            // Admin sees all projects
            var allProjects = await _projectService.GetAllProjectsAsync();
            return Ok(allProjects.Select(p => new ProjectResponse(
                p.Id, p.Title, p.Description, p.Status, p.CreatedAt, p.UpdatedAt)));
        }

        // Regular users see only their own
        var myProjects = await _projectService.GetMyProjectsAsync(_currentUser.DbId);
        return Ok(myProjects.Select(p => new ProjectResponse(
            p.Id, p.Title, p.Description, p.Status, p.CreatedAt, p.UpdatedAt)));
    }

    /// <summary>
    /// Get a single project by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var project = await _projectService.GetAsync(id, _currentUser.DbId, _currentUser.IsAdmin);
        if (project == null) return NotFound();

        return Ok(new ProjectResponse(
            project.Id, project.Title, project.Description, 
            project.Status, project.CreatedAt, project.UpdatedAt));
    }

    /// <summary>
    /// Create a new project.
    /// Requires write access (NotGuest policy).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.NotGuest)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var project = await _projectService.CreateAsync(_currentUser.DbId, request);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, 
            new ProjectResponse(project.Id, project.Title, project.Description, 
                project.Status, project.CreatedAt, project.UpdatedAt));
    }

    /// <summary>
    /// Update a project.
    /// Owner or admin only.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.NotGuest)]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var project = await _projectService.UpdateAsync(
                id, _currentUser.DbId, _currentUser.IsAdmin, request);
            return Ok(new ProjectResponse(
                project.Id, project.Title, project.Description, 
                project.Status, project.CreatedAt, project.UpdatedAt));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Delete a project.
    /// Owner or admin only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.NotGuest)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        try
        {
            await _projectService.DeleteAsync(id, _currentUser.DbId, _currentUser.IsAdmin);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
