using Microsoft.AspNetCore.Mvc;
using Trackr.Api.Dtos;
using Trackr.Api.Services;
using Trackr.Api.Models;

namespace Trackr.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId}/issues")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;

    public IssuesController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    [HttpGet]
    public async Task<IActionResult> GetIssues(
        int projectId,
        [FromQuery] IssueStatus? status,
        [FromQuery] IssuePriority? priority
        )
    {
        var issues = await _issueService.GetIssuesByProjectAsync(
            projectId,
            status,
            priority
        );
        
        return Ok(issues);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetIssueById(int projectId, int id)
    {
        var issue = await _issueService.GetIssueByIdAsync(projectId, id);

        if (issue is null)
        {
            return NotFound();
        }

        return Ok(issue);
    }

    [HttpPost]
    public async Task<IActionResult> CreateIssue(int projectId, CreateIssueRequest request)
    {
        var issue = await _issueService.CreateIssueAsync(projectId, request);

        if (issue is null)
        {
            return NotFound();
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
            new { 
                    projectId = issue.ProjectId, 
                    id = issue.Id 
                }, 
                response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIssue(int projectId, int id, UpdateIssueRequest request)
    {
        var updated = await _issueService.UpdateIssueAsync(projectId, id, request);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIssue(int projectId, int id)
    {
        var deleted = await _issueService.DeleteIssueAsync(projectId, id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}