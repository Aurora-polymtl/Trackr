using Trackr.Api.Models;

namespace Trackr.Api.Services;

public interface IProjectService
{
    IEnumerable<Project> GetProjects();
}