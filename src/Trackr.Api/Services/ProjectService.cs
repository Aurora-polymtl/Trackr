using Microsoft.EntityFrameworkCore;
using Trackr.Api.Data;
using Trackr.Api.Models;
using Trackr.Api.Dtos;

namespace Trackr.Api.Services;

public class ProjectService: IProjectService
{
    private readonly TrackrDbContext _dbContext;

    public ProjectService(TrackrDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Project>> GetProjectsAsync()
    {
        return await _dbContext.Projects.ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await _dbContext.Projects.FindAsync(id);
    }

    public async Task<Project> CreateProjectAsync(CreateProjectRequest request)
    {
        var now = DateTime.UtcNow;
        
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Projects.Add(project);

        await _dbContext.SaveChangesAsync();

        return project;
    }

    public async Task<bool> UpdateProjectAsync(int id, UpdateProjectRequest request)
    {
        var project = await _dbContext.Projects.FindAsync(id);

        if (project is null)
        {
            return false;
        }

        project.Name = request.Name;
        project.Description = request.Description;
        project.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        var project = await _dbContext.Projects.FindAsync(id);

        if (project is null)
        {
            return false;
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}