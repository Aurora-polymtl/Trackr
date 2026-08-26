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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProjectById(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);

        if (project is null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject(CreateProjectRequest request)
    {
        var project = await _projectService.CreateProjectAsync(request);

        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(int id, UpdateProjectRequest request)
    {
        var updated = await _projectService.UpdateProjectAsync(id, request);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var deleted = await _projectService.DeleteProjectAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
