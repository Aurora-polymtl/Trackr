using Trackr.Api.Models;
using Trackr.Api.Dtos;

namespace Trackr.Api.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetProjectsAsync();

    Task<Project?> GetProjectByIdAsync(int id);

    Task<Project> CreateProjectAsync(CreateProjectRequest request);
}