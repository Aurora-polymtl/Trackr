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

    public async Task<PagedResponse<IssueResponse>?> GetIssuesByProjectAsync(
        int projectId,
        IssueQueryParameters queryParameters,
        string userId
        )
    {
        var projectExists = await _dbContext.Projects.AnyAsync(project =>
            project.Id == projectId &&
            (
                project.UserId == userId ||
                project.Members.Any(member =>
                    member.UserId == userId)
            ));

        if (!projectExists)
        {
            return null;
        }

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
                ProjectId = issue.ProjectId,
                AssigneeId = issue.AssigneeId
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

    public async Task<IssueResponse?> GetIssueByIdAsync(int projectId, int id, string userId)
    {
        return await _dbContext.Issues
            .Where(issue =>
                issue.Id == id &&
                issue.ProjectId == projectId &&
                (
                    issue.Project.UserId == userId ||
                    issue.Project.Members.Any(member =>
                        member.UserId == userId)
                ))
            .Select(issue => new IssueResponse
            {
                Id = issue.Id,
                Title = issue.Title,
                Description = issue.Description,
                Status = issue.Status,
                Priority = issue.Priority,
                CreatedAt = issue.CreatedAt,
                UpdatedAt = issue.UpdatedAt,
                ProjectId = issue.ProjectId,
                AssigneeId = issue.AssigneeId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<IssueCommentResponse>?> GetIssueCommentsAsync(int projectId, int issueId, string userId)
    {
        var issueExists = await _dbContext.Issues.AnyAsync(issue =>
            issue.Id == issueId &&
            issue.ProjectId == projectId &&
            (
                issue.Project.UserId == userId ||
                issue.Project.Members.Any(member =>
                    member.UserId == userId)
            ));

        if (!issueExists)
        {
            return null;
        }

        return await _dbContext.IssueComments
            .Where(comment => comment.IssueId == issueId)
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .Select(comment => new IssueCommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IssueId = comment.IssueId,
                AuthorId = comment.AuthorId
            })
            .ToListAsync();
    }

    public async Task<IssueCommentResponse?> GetIssueCommentByIdAsync(int projectId, int issueId, int commentId, string userId)
    {
        return await _dbContext.IssueComments
            .Where(comment =>
                comment.Id == commentId &&
                comment.IssueId == issueId &&
                comment.Issue.ProjectId == projectId &&
                (
                    comment.Issue.Project.UserId == userId ||
                    comment.Issue.Project.Members.Any(member =>
                        member.UserId == userId)
                ))
            .Select(comment => new IssueCommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IssueId = comment.IssueId,
                AuthorId = comment.AuthorId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(IssueOperationResult Result, Issue? Issue)> CreateIssueAsync(
        int projectId,
        CreateIssueRequest request,
        string userId)
    {
        var projectExists = await _dbContext.Projects
            .AnyAsync(project =>
                project.Id == projectId &&
                project.UserId == userId);

        if (!projectExists)
        {
            return (IssueOperationResult.ProjectNotFound, null);
        }

        if (request.AssigneeId is not null && 
            !await IsValidAssigneeAsync(projectId, request.AssigneeId))
        {
            return (IssueOperationResult.InvalidAssignee, null);
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
            AssigneeId = request.AssigneeId
        };

        _dbContext.Issues.Add(issue);
        await _dbContext.SaveChangesAsync();

        return (IssueOperationResult.Success, issue);
    }

    public async Task<(IssueOperationResult Result, IssueComment? Comment)> CreateIssueCommentAsync(
        int projectId,
        int issueId,
        CreateIssueCommentRequest request,
        string userId
    )
    {
        var issueExists = await _dbContext.Issues.AnyAsync(issue =>
            issue.Id == issueId &&
            issue.ProjectId == projectId &&
            (
                issue.Project.UserId == userId ||
                issue.Project.Members.Any(member =>
                    member.UserId == userId)
            ));
        if (!issueExists)
        {
            return (IssueOperationResult.IssueNotFound, null);
        }

        var now = DateTime.UtcNow;
        var comment = new IssueComment
        {
            Content = request.Content,
            CreatedAt = now,
            UpdatedAt = now,
            IssueId = issueId,
            AuthorId = userId
        };
        _dbContext.IssueComments.Add(comment);
        await _dbContext.SaveChangesAsync();

        return (IssueOperationResult.Success, comment);
    }

    public async Task<IssueOperationResult> UpdateIssueAsync(int projectId, int id, UpdateIssueRequest request, string userId)
    {
        var issue = await _dbContext.Issues
            .FirstOrDefaultAsync(issue =>
                issue.Id == id &&
                issue.ProjectId == projectId &&
                issue.Project.UserId == userId);

        if (issue is null)
        {
            return IssueOperationResult.IssueNotFound;
        }

        if (request.AssigneeId is not null && 
            !await IsValidAssigneeAsync(projectId, request.AssigneeId))
        {
            return IssueOperationResult.InvalidAssignee;
        }

        issue.Title = request.Title;
        issue.Description = request.Description;
        issue.Status = request.Status;
        issue.Priority = request.Priority;
        issue.UpdatedAt = DateTime.UtcNow;
        issue.AssigneeId = request.AssigneeId;

        await _dbContext.SaveChangesAsync();

        return IssueOperationResult.Success;
    }

    public async Task<bool> UpdateIssueCommentAsync(
        int projectId,
        int issueId,
        int commentId,
        UpdateIssueCommentRequest request,
        string userId
    )
    {
        var comment = await _dbContext.IssueComments
            .FirstOrDefaultAsync(comment =>
                comment.Id == commentId &&
                comment.IssueId == issueId &&
                comment.Issue.ProjectId == projectId &&
                comment.AuthorId == userId &&
                (
                    comment.Issue.Project.UserId == userId ||
                    comment.Issue.Project.Members.Any(member =>
                        member.UserId == userId)
                ));

        if (comment is null)
        {
            return false;
        }

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteIssueAsync(int projectId, int id, string userId)
    {
        var issue = await _dbContext.Issues
            .FirstOrDefaultAsync(issue =>
                issue.Id == id &&
                issue.ProjectId == projectId &&
                issue.Project.UserId == userId);

        if (issue is null)
        {
            return false;
        }

        _dbContext.Issues.Remove(issue);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteIssueCommentAsync(int projectId, int issueId, int commentId, string userId)
    {
        var comment = await _dbContext.IssueComments.FirstOrDefaultAsync(comment =>
            comment.Id == commentId &&
            comment.IssueId == issueId &&
            comment.Issue.ProjectId == projectId &&
            comment.AuthorId == userId &&
            (
                comment.Issue.Project.UserId == userId ||
                comment.Issue.Project.Members.Any(member =>
                    member.UserId == userId)
            ));

        if (comment is null)
        {
            return false;
        }

        _dbContext.IssueComments.Remove(comment);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    private async Task<bool> IsValidAssigneeAsync(int projectId, string assigneeId)
    {
        return await _dbContext.Projects.AnyAsync(project =>
            project.Id == projectId &&
            (
                project.UserId == assigneeId ||
                project.Members.Any(member =>
                    member.UserId == assigneeId)
            ));
    }
}