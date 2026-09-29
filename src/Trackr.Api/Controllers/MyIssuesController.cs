using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackr.Api.Dtos;
using Trackr.Api.Services;

namespace Trackr.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/issues")]
public class MyIssuesController : ControllerBase
{
    private readonly IIssueService _issueService;

    public MyIssuesController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    [HttpGet("assigned-to-me")]
    public async Task<IActionResult> GetAssignedToMe([FromQuery] AssignedIssuesQueryParameters queryParameters)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? throw new InvalidOperationException("Authenticated user has no identifier.");
        
        var response = await _issueService.GetAssignedIssuesAsync(queryParameters, userId);

        return Ok(response);
    }
}