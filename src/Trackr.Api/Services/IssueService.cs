using Microsoft.EntityFrameworkCore;
using Trackr.Api.Data;
using Trackr.Api.Dtos;
using Trackr.Api.Models;

namespace Trackr.Api.Services;

public class IssueService : IIssueService
{
    private readonly TrackrDbContext _dbContext;

    public IssueService(TrackrDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<IssueResponse>> GetIssuesByProjectAsync(int projectId)
    {
        return await _dbContext.Issues
            .Where(issue => issue.ProjectId == projectId)
            .Select(issue => new IssueResponse
            {
                Id = issue.Id,
                Title = issue.Title,
                Description = issue.Description,
                Status = issue.Status,
                Priority = issue.Priority,
                CreatedAt = issue.CreatedAt,
                ProjectId = issue.ProjectId
            })
            .ToListAsync();
    }

    public async Task<IssueResponse?> GetIssueByIdAsync(int projectId, int id)
    {
        return await _dbContext.Issues
            .Where(issue => issue.Id == id && issue.ProjectId == projectId)
            .Select(issue => new IssueResponse
            {
                Id = issue.Id,
                Title = issue.Title,
                Description = issue.Description,
                Status = issue.Status,
                Priority = issue.Priority,
                CreatedAt = issue.CreatedAt,
                ProjectId = issue.ProjectId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Issue?> CreateIssueAsync(int projectId, CreateIssueRequest request)
    {
        var project = await _dbContext.Projects.FindAsync(projectId);

        if (project is null)
        {
            return null;
        }

        var issue = new Issue
        {
            Title = request.Title,
            Description = request.Description,
            Status = IssueStatus.Backlog,
            Priority = request.Priority,
            CreatedAt = DateTime.UtcNow,
            ProjectId = projectId,
        };

        _dbContext.Issues.Add(issue);
        await _dbContext.SaveChangesAsync();

        return issue;
    }

    public async Task<bool> UpdateIssueAsync(int projectId, int id, UpdateIssueRequest request)
    {
        var issue = await _dbContext.Issues
            .FirstOrDefaultAsync(issue => issue.Id == id && issue.ProjectId == projectId);

        if (issue is null)
        {
            return false;
        }

        issue.Title = request.Title;
        issue.Description = request.Description;
        issue.Status = request.Status;
        issue.Priority = request.Priority;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteIssueAsync(int projectId, int id)
    {
        var issue = await _dbContext.Issues
            .FirstOrDefaultAsync(issue => issue.Id == id && issue.ProjectId == projectId);

        if (issue is null)
        {
            return false;
        }

        _dbContext.Issues.Remove(issue);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}