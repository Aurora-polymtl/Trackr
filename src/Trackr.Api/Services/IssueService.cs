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

    public async Task<PagedResponse<IssueResponse>> GetIssuesByProjectAsync(
        int projectId,
        IssueQueryParameters queryParameters
        )
    {
        var query = _dbContext.Issues
            .Where(issue => issue.ProjectId == projectId)
            .AsQueryable();

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(issue => issue.Status == queryParameters.Status.Value);
        }

        if (queryParameters.Priority.HasValue)
        {
            query = query.Where(issue => issue.Priority == queryParameters.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
        {
            query = query.Where(issue => 
                issue.Title.Contains(queryParameters.Search) || 
                issue.Description.Contains(queryParameters.Search));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)queryParameters.PageSize);

        query = queryParameters.SortBy switch
        {
            IssueSortBy.UpdatedAt =>
                queryParameters.SortDirection == SortDirection.Asc
                    ? query.OrderBy(issue => issue.UpdatedAt)
                    : query.OrderByDescending(issue => issue.UpdatedAt),
            IssueSortBy.Priority =>
                queryParameters.SortDirection == SortDirection.Asc
                    ? query.OrderBy(issue => 
                        issue.Priority == IssuePriority.Low ? 0 :
                        issue.Priority == IssuePriority.Medium ? 1 : 
                        issue.Priority == IssuePriority.High ? 2 : 
                        issue.Priority == IssuePriority.Critical ? 3 :
                        4)
                    : query.OrderByDescending(issue => 
                        issue.Priority == IssuePriority.Low ? 0 :
                        issue.Priority == IssuePriority.Medium ? 1 : 
                        issue.Priority == IssuePriority.High ? 2 : 
                        issue.Priority == IssuePriority.Critical ? 3 :
                        4),
            IssueSortBy.Status =>
                queryParameters.SortDirection == SortDirection.Asc
                    ? query.OrderBy(issue => issue.Status)
                    : query.OrderByDescending(issue => issue.Status),
            _ =>
                queryParameters.SortDirection == SortDirection.Asc
                    ? query.OrderBy(issue => issue.CreatedAt)
                    : query.OrderByDescending(issue => issue.CreatedAt)
        };

        var items = await query
            .Skip((queryParameters.Page - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .Select(issue => new IssueResponse
            {
                Id = issue.Id,
                Title = issue.Title,
                Description = issue.Description,
                Status = issue.Status,
                Priority = issue.Priority,
                CreatedAt = issue.CreatedAt,
                UpdatedAt = issue.UpdatedAt,
                ProjectId = issue.ProjectId
            })
            .ToListAsync();

        return new PagedResponse<IssueResponse>
        {
            Items = items,
            Page = queryParameters.Page,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
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
                UpdatedAt = issue.UpdatedAt,
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

        var now = DateTime.UtcNow;

        var issue = new Issue
        {
            Title = request.Title,
            Description = request.Description,
            Status = IssueStatus.Backlog,
            Priority = request.Priority,
            CreatedAt = now,
            UpdatedAt = now,
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
        issue.UpdatedAt = DateTime.UtcNow;

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