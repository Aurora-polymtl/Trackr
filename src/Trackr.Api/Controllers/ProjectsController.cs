using Microsoft.AspNetCore.Mvc;
using Trackr.Api.Services;
using Trackr.Api.Dtos;

namespace Trackr.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var projects = await _projectService.GetProjectsAsync();

        return Ok(projects);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject(CreateProjectRequest request)
    {
        var project = await _projectService.CreateProjectAsync(request);

        return Ok(project);
    }
}
