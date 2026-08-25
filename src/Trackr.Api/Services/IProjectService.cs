using Trackr.Api.Models;

namespace Trackr.Api.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetProjectsAsync();
}