using Trackr.Api.Models;
using Trackr.Api.Dtos;

namespace Trackr.Api.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponse>> GetProjectsAsync(string userId);

    Task<ProjectResponse?> GetProjectByIdAsync(int id, string userId);

    Task<IReadOnlyList<ProjectMemberResponse>?> GetProjectMembersAsync(int projectId, string userId);

    Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request, string userId);

    Task<bool> UpdateProjectAsync(int id, UpdateProjectRequest request, string userId);

    Task<bool> DeleteProjectAsync(int id, string userId);
    Task<(ProjectMemberOperationResult Result, ProjectMemberResponse? Member)> AddProjectMemberAsync(
        int projectId,
        AddProjectMemberRequest request,
        string ownerId
    );
}