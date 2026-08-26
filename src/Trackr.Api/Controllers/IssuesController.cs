using Microsoft.AspNetCore.Mvc;
using Trackr.Api.Dtos;
using Trackr.Api.Services;

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
    public async Task<IActionResult> GetIssues(int projectId)
    {
        var issues = await _issueService.GetIssuesByProjectAsync(projectId);
        return Ok(issues);
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
            CreatedAt = issue.CreatedAt,
            ProjectId = issue.ProjectId
        };

        return Ok(response);
    }
}