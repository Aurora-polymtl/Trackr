using Trackr.Api.Dtos;
using Trackr.Api.Models;

namespace Trackr.Api.Services;

public interface IIssueService
{
    Task<IEnumerable<IssueResponse>> GetIssuesByProjectAsync(int projectId);
    Task<Issue?> CreateIssueAsync(int projectId, CreateIssueRequest request);
}