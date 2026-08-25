using Microsoft.EntityFrameworkCore;
using Trackr.Api.Data;
using Trackr.Api.Models;

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
}