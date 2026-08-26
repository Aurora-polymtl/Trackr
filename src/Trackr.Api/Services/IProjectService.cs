using Trackr.Api.Models;
using Trackr.Api.Dtos;

namespace Trackr.Api.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetProjectsAsync();

    Task<Project?> GetProjectByIdAsync(int id);

    Task<Project> CreateProjectAsync(CreateProjectRequest request);

    Task<bool> UpdateProjectAsync(int id, UpdateProjectRequest request);

    Task<bool> DeleteProjectAsync(int id);
}