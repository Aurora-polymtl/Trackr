using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Trackr.Api.Services;
using Trackr.Api.Dtos;

namespace Trackr.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user has no identifier.");

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var projects = await _projectService.GetProjectsAsync(CurrentUserId);

        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProjectById(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id, CurrentUserId);

        if (project is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"Project with id {id} was not found.");
        }

        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject(CreateProjectRequest request)
    {
        var project = await _projectService.CreateProjectAsync(request, CurrentUserId);

        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddProjectMember(int id, AddProjectMemberRequest request)
    {
        var (result, member) = await _projectService.AddProjectMemberAsync(id, request, CurrentUserId);

        if (result == ProjectMemberOperationResult.ProjectNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"Project with id {id} was not found."
            );
        }

        if (result is ProjectMemberOperationResult.UserNotFound or ProjectMemberOperationResult.OwnerCannotBeMember)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid member",
                detail: "The specified user cannot be added to the project."
            );
        }

        if (result == ProjectMemberOperationResult.MemberAlreadyExists)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Member already exists",
                detail: "The specified user is already a project member."
            );
        }

        if (member is null)
        {
            throw new InvalidOperationException("Member creation succeeded without returning a member.");
        }

        return Created($"/api/projects/{id}/members/{member.UserId}", member);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(int id, UpdateProjectRequest request)
    {
        var updated = await _projectService.UpdateProjectAsync(id, request, CurrentUserId);

        if (!updated)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"Project with id {id} was not found.");
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var deleted = await _projectService.DeleteProjectAsync(id, CurrentUserId);

        if (!deleted)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"Project with id {id} was not found.");
        }

        return NoContent();
    }
}
