using Trackr.Api.Dtos;
using Trackr.Api.Models;

namespace Trackr.Api.Services;

public interface IIssueService
{
    Task<PagedResponse<IssueResponse>> GetIssuesByProjectAsync(
        int projectId, 
        IssueQueryParameters queryParameters
        );
    Task<IssueResponse?> GetIssueByIdAsync(int projectId, int id);
    Task<Issue?> CreateIssueAsync(int projectId, CreateIssueRequest request);
    Task<bool> UpdateIssueAsync(int projectId, int id, UpdateIssueRequest request);
    Task<bool> DeleteIssueAsync(int projectId, int id);
}