using Trackr.Api.Models;
using Trackr.Api.Dtos;

namespace Trackr.Api.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponse>> GetProjectsAsync();

    Task<ProjectResponse?> GetProjectByIdAsync(int id);

    Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request);

    Task<bool> UpdateProjectAsync(int id, UpdateProjectRequest request);

    Task<bool> DeleteProjectAsync(int id);
}