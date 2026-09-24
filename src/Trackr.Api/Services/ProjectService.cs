using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Trackr.Api.Data;
using Trackr.Api.Models;
using Trackr.Api.Dtos;

namespace Trackr.Api.Services;

public class ProjectService : IProjectService
{
    private readonly TrackrDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProjectService(TrackrDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IEnumerable<ProjectResponse>> GetProjectsAsync(string userId)
    {
        return await _dbContext.Projects
            .Where(project => 
                project.UserId == userId ||
                project.Members.Any(member =>
                    member.UserId == userId))
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                IsOwner = project.UserId == userId
            })
            .ToListAsync();
    }

    public async Task<ProjectResponse?> GetProjectByIdAsync(int id, string userId)
    {
        return await _dbContext.Projects
            .Where(project => project.Id == id && 
                (
                    project.UserId == userId ||
                    project.Members.Any(member =>
                        member.UserId == userId)
                ))
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                IsOwner = project.UserId == userId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ProjectIssueSummaryResponse?> GetProjectIssueSummaryAsync(int projectId, string userId)
    {
        var projectIsAccessible = await _dbContext.Projects.AnyAsync(project =>
            project.Id == projectId &&
            (
                project.UserId == userId ||
                project.Members.Any(member => member.UserId == userId)
            ));

        if (!projectIsAccessible)
        {
            return null;
        }

        var counts = await _dbContext.Issues
            .Where(issue => issue.ProjectId == projectId)
            .GroupBy(issue => issue.Status)
            .Select(group => new
            {
                Status = group.Key, Count = group.Count()
            })
            .ToDictionaryAsync(row => row.Status, row => row.Count);
        
        return new ProjectIssueSummaryResponse
        {
            ProjectId = projectId,
            TotalIssues = counts.Values.Sum(),
            BacklogCount = counts.GetValueOrDefault(IssueStatus.Backlog),
            TodoCount = counts.GetValueOrDefault(IssueStatus.Todo),
            InProgressCount = counts.GetValueOrDefault(IssueStatus.InProgress),
            ReviewCount = counts.GetValueOrDefault(IssueStatus.Review),
            DoneCount = counts.GetValueOrDefault(IssueStatus.Done)
        };
    }

    public async Task<IReadOnlyList<ProjectMemberResponse>?> GetProjectMembersAsync(int projectId, string userId)
    {
        var projectIsAccessible = await _dbContext.Projects.AnyAsync(project =>
            project.Id == projectId &&
            (
                project.UserId == userId ||
                project.Members.Any(member =>
                    member.UserId == userId)
            ));
        
        if (!projectIsAccessible)
        {
            return null;
        }

        return await _dbContext.ProjectMembers
            .Where(member => member.ProjectId == projectId)
            .OrderBy(member => member.User.Email)
            .Select(member => new ProjectMemberResponse
            {
                UserId = member.UserId,
                Email = member.User.Email ?? string.Empty,
                AddedAt = member.AddedAt
            })
            .ToListAsync();
    }

    public async Task<ProjectMemberResponse?> GetProjectMemberByIdAsync(int projectId, string memberId, string userId)
    {
        return await _dbContext.ProjectMembers
            .Where(member =>
                member.ProjectId == projectId &&
                member.UserId == memberId &&
                (
                    member.Project.UserId == userId ||
                    member.Project.Members.Any(projectMember =>
                        projectMember.UserId == userId)
                ))
            .Select(member => new ProjectMemberResponse
            {
                UserId = member.UserId,
                Email = member.User.Email ?? string.Empty,
                AddedAt = member.AddedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request, string userId)
    {
        var now = DateTime.UtcNow;

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = now,
            UpdatedAt = now,
            UserId = userId
        };

        _dbContext.Projects.Add(project);

        await _dbContext.SaveChangesAsync();

        return new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            IsOwner = true
        };
    }

    public async Task<(ProjectMemberOperationResult Result, ProjectMemberResponse? Member)> AddProjectMemberAsync(
        int projectId,
        AddProjectMemberRequest request,
        string ownerId
    )
    {
        var projectExists = await _dbContext.Projects.AnyAsync(project =>
            project.Id == projectId &&
            project.UserId == ownerId);
        
        if (!projectExists)
        {
            return (ProjectMemberOperationResult.ProjectNotFound, null);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return (ProjectMemberOperationResult.UserNotFound, null);
        }

        if (user.Id == ownerId)
        {
            return (ProjectMemberOperationResult.OwnerCannotBeMember, null);
        }

        var alreadyExists = await _dbContext.ProjectMembers.AnyAsync(member =>
            member.ProjectId == projectId &&
            member.UserId == user.Id);

        if (alreadyExists)
        {
            return (ProjectMemberOperationResult.MemberAlreadyExists, null);
        }

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = user.Id,
            AddedAt = DateTime.UtcNow
        };

        _dbContext.ProjectMembers.Add(member);
        await _dbContext.SaveChangesAsync();

        var response = new ProjectMemberResponse
        {
            UserId = user.Id,
            Email = user.Email ?? request.Email,
            AddedAt = member.AddedAt
        };

        return (ProjectMemberOperationResult.Success, response);
    }

    public async Task<bool> UpdateProjectAsync(int id, UpdateProjectRequest request, string userId)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(project =>
            project.Id == id &&
            project.UserId == userId
            );

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

    public async Task<bool> DeleteProjectAsync(int id, string userId)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(project =>
                project.Id == id &&
                project.UserId == userId);

        if (project is null)
        {
            return false;
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveProjectMemberAsync(int projectId, string memberId, string ownerId)
    {
        var member = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(member =>
                member.ProjectId == projectId &&
                member.UserId == memberId &&
                member.Project.UserId == ownerId);

        if (member is null)
        {
            return false;
        }

        var assignedIssues = await _dbContext.Issues
            .Where(issue =>
                issue.ProjectId == projectId &&
                issue.AssigneeId == memberId)
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var issue in assignedIssues)
        {
            issue.AssigneeId = null;
            issue.UpdatedAt = now;
        }

        _dbContext.ProjectMembers.Remove(member);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}