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

    public async Task<IEnumerable<ProjectResponse>> GetProjectsAsync()
    {
        return await _dbContext.Projects
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<ProjectResponse?> GetProjectByIdAsync(int id)
    {
        return await _dbContext.Projects
            .Where(project => project.Id == id)
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request)
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

        return new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
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