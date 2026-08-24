using Microsoft.AspNetCore.Mvc;
using Trackr.Api.Services;

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
    public IActionResult GetProjects()
    {
        var projects = _projectService.GetProjects();

        return Ok(projects);
    }
}
