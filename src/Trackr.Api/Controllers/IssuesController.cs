using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Trackr.Api.Dtos;
using Trackr.Api.Services;
using Trackr.Api.Models;

namespace Trackr.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/projects/{projectId}/issues")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user has no identifier.");

    public IssuesController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    [HttpGet]
    public async Task<IActionResult> GetIssues(
        int projectId,
        [FromQuery] IssueQueryParameters queryParameters
        )
    {
        var response = await _issueService.GetIssuesByProjectAsync(
            projectId,
            queryParameters,
            CurrentUserId
        );

        if (response is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"Project with id {projectId} was not found.");
        }

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetIssueById(int projectId, int id)
    {
        var issue = await _issueService.GetIssueByIdAsync(projectId, id, CurrentUserId);

        if (issue is null)
        {
            return NotFound();
        }

        return Ok(issue);
    }

    [HttpPost]
    public async Task<IActionResult> CreateIssue(int projectId, CreateIssueRequest request)
    {
        var issue = await _issueService.CreateIssueAsync(projectId, request, CurrentUserId);

        if (issue is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"Project with id {projectId} was not found.");
        }

        var response = new IssueResponse
        {
            Id = issue.Id,
            Title = issue.Title,
            Description = issue.Description,
            Status = issue.Status,
            Priority = issue.Priority,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt,
            ProjectId = issue.ProjectId
        };

        return CreatedAtAction(
            nameof(GetIssueById),
            new
            {
                projectId = issue.ProjectId,
                id = issue.Id
            },
                response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIssue(int projectId, int id, UpdateIssueRequest request)
    {
        var updated = await _issueService.UpdateIssueAsync(projectId, id, request, CurrentUserId);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIssue(int projectId, int id)
    {
        var deleted = await _issueService.DeleteIssueAsync(projectId, id, CurrentUserId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}