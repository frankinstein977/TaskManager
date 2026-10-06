using Microsoft.EntityFrameworkCore;
using EntraIdPoc.Api.Models;
using EntraIdPoc.Api.Data;

namespace EntraIdPoc.Api.Services;

public interface IProjectService
{
    Task<Project> CreateAsync(Guid ownerId, CreateProjectRequest request, CancellationToken ct = default);
    Task<Project?> GetAsync(Guid projectId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
    Task<List<Project>> GetMyProjectsAsync(Guid ownerId, CancellationToken ct = default);
    Task<List<Project>> GetAllProjectsAsync(CancellationToken ct = default);
    Task<Project> UpdateAsync(Guid projectId, Guid requestingUserId, bool isAdmin, UpdateProjectRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid projectId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
}

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ProjectService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Project> CreateAsync(Guid ownerId, CreateProjectRequest request, CancellationToken ct = default)
    {
        var project = new Project
        {
            OwnerId = ownerId,
            Title = request.Title,
            Description = request.Description
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ownerId, "PROJECT_CREATED", 
            $"{{"projectId":"{project.Id}","title":"{request.Title}"}}", ct);

        return project;
    }

    public async Task<Project?> GetAsync(Guid projectId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var project = await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        if (project == null) return null;

        if (!isAdmin && project.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("You can only view your own projects");

        return project;
    }

    public async Task<List<Project>> GetMyProjectsAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _db.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Project>> GetAllProjectsAsync(CancellationToken ct = default)
    {
        return await _db.Projects
            .AsNoTracking()
            .Include(p => p.Owner)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Project> UpdateAsync(Guid projectId, Guid requestingUserId, bool isAdmin, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await _db.Projects.FindAsync(new object[] { projectId }, ct)
            ?? throw new InvalidOperationException("Project not found");

        if (!isAdmin && project.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("You can only edit your own projects");

        project.Title = request.Title;
        project.Description = request.Description;
        project.Status = request.Status;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(requestingUserId, "PROJECT_UPDATED", 
            $"{{"projectId":"{projectId}"}}", ct);

        return project;
    }

    public async Task DeleteAsync(Guid projectId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var rows = await _db.Database.ExecuteSqlAsync($"""
            DELETE FROM projects 
            WHERE id = {projectId} 
            AND ({isAdmin} = TRUE OR owner_id = {requestingUserId})
        """, ct);

        if (rows == 0)
            throw new InvalidOperationException("Project not found or access denied");

        await _audit.LogAsync(requestingUserId, "PROJECT_DELETED", 
            $"{{"projectId":"{projectId}"}}", ct);
    }
}
