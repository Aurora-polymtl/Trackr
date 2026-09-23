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

    [HttpGet("{issueId}/comments")]
    public async Task<IActionResult> GetIssueComments(int projectId, int issueId)
    {
        var comments = await _issueService.GetIssueCommentsAsync(projectId, issueId, CurrentUserId);

        if (comments is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Issue not found",
                detail: $"Issue with id {issueId} was not found."
            );
        }

        return Ok(comments);
    }

    [HttpGet("{issueId}/comments/{commentId}")]
    public async Task<IActionResult> GetIssueCommentById(int projectId, int issueId, int commentId)
    {
        var comment = await _issueService.GetIssueCommentByIdAsync(projectId, issueId, commentId, CurrentUserId);

        if (comment is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Comment not found",
                detail: $"Comment with id {commentId} was not found."
            );
        }
        return Ok(comment);
    }

    [HttpPost]
    public async Task<IActionResult> CreateIssue(int projectId, CreateIssueRequest request)
    {
        var (result, issue) = await _issueService.CreateIssueAsync(projectId, request, CurrentUserId);

        if (result == IssueOperationResult.ProjectNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"Project with id {projectId} was not found.");
        }

        if (result == IssueOperationResult.InvalidAssignee)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid assignee",
                detail: "The issue can only be assigned to the project owner or a project member."
            );
        }

        if (issue is null)
        {
            throw new InvalidOperationException("Issue creation succeeded without returning an issue.");
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
            ProjectId = issue.ProjectId,
            AssigneeId = issue.AssigneeId
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

    [HttpPost("{issueId}/comments")]
    public async Task<IActionResult> CreateIssueComment(
        int projectId,
        int issueId,
        CreateIssueCommentRequest request
    )
    {
        var (result, comment) = await _issueService.CreateIssueCommentAsync(
            projectId,
            issueId,
            request,
            CurrentUserId
        );

        if (result == IssueOperationResult.IssueNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Issue not found",
                detail: $"Issue with id {issueId} was not found."
            );
        }

        if (comment is null)
        {
            throw new InvalidOperationException("Comment creation succeeded without returning a comment.");
        }

        var response = new IssueCommentResponse
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            IssueId = comment.IssueId,
            AuthorId = comment.AuthorId
        };

        return CreatedAtAction(
            nameof(GetIssueCommentById),
            new
            {
                projectId,
                issueId,
                commentId = comment.Id
            },
            response
        );
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIssue(int projectId, int id, UpdateIssueRequest request)
    {
        var result = await _issueService.UpdateIssueAsync(projectId, id, request, CurrentUserId);

        if (result == IssueOperationResult.InvalidAssignee)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid assignee",
                detail: "The issue can only be assigned to the project owner or a project member."
            );
        }

        if (result != IssueOperationResult.Success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateIssueStatus(int projectId, int id, UpdateIssueStatusRequest request)
    {
        var result = await _issueService.UpdateIssueStatusAsync(projectId, id, request.Status, CurrentUserId);

        if (result != IssueOperationResult.Success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{issueId}/comments/{commentId}")]
    public async Task<IActionResult> UpdateIssueComment(
        int projectId, 
        int issueId, 
        int commentId, 
        UpdateIssueCommentRequest request)
    {
        var updated = await _issueService.UpdateIssueCommentAsync(
            projectId,
            issueId,
            commentId,
            request,
            CurrentUserId
        );

        if (!updated)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Comment not found",
                detail: $"Comment with id {commentId} was not found."
            );
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

    [HttpDelete("{issueId}/comments/{commentId}")]
    public async Task<IActionResult> DeleteIssueComment(int projectId, int issueId, int commentId)
    {
        var deleted = await _issueService.DeleteIssueCommentAsync(
            projectId,
            issueId,
            commentId,
            CurrentUserId
        );

        if (!deleted)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Comment not found",
                detail: $"Comment with id {commentId} was not found."
            );
        }

        return NoContent();
    }
}