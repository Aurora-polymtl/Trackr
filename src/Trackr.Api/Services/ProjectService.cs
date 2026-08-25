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
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Projects.Add(project);

        await _dbContext.SaveChangesAsync();

        return project;
    }
}