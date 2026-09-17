using Trackr.Api.Dtos;
using Trackr.Api.Models;

namespace Trackr.Api.Services;

public interface IIssueService
{
    Task<PagedResponse<IssueResponse>?> GetIssuesByProjectAsync(
        int projectId, 
        IssueQueryParameters queryParameters,
        string userId
        );
    Task<IssueResponse?> GetIssueByIdAsync(int projectId, int id, string userId);
    Task<IReadOnlyList<IssueCommentResponse>?> GetIssueCommentsAsync(int projectId, int issueId, string userId);
    Task<(IssueOperationResult Result, Issue? Issue)> CreateIssueAsync(int projectId, CreateIssueRequest request, string userId);
    Task<(IssueOperationResult Result, IssueComment? Comment)> CreateIssueCommentAsync(
        int projectId, 
        int issueId, 
        CreateIssueCommentRequest request, 
        string userId);
    Task<IssueOperationResult> UpdateIssueAsync(int projectId, int id, UpdateIssueRequest request, string userId);
    Task<bool> DeleteIssueAsync(int projectId, int id, string userId);
}