using Trackr.Api.Dtos;
using Trackr.Api.Models;
using Trackr.Api.Services;

namespace Trackr.Api.Tests.Integration;

public class ThrowingIssueService : IIssueService
{
    public Task<PagedResponse<IssueResponse>?> GetIssuesByProjectAsync(
        int projectId,
        IssueQueryParameters queryParameters,
        string userId)
    {
        throw new InvalidOperationException(
            "Simulated test exception.");
    }

    public Task<IssueResponse?> GetIssueByIdAsync(
        int projectId,
        int id,
        string userId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<IssueCommentResponse>?>
    GetIssueCommentsAsync(
        int projectId,
        int issueId,
        string userId)
    {
        throw new NotImplementedException();
    }

    public Task<IssueCommentResponse?> GetIssueCommentByIdAsync(
    int projectId,
    int issueId,
    int commentId,
    string userId)
    {
        throw new NotImplementedException();
    }

    public Task<(IssueOperationResult Result, Issue? Issue)> CreateIssueAsync(
        int projectId,
        CreateIssueRequest request,
        string userId)
    {
        throw new NotImplementedException();
    }

    public Task<(IssueOperationResult Result, IssueComment? Comment)> CreateIssueCommentAsync(
        int projectId,
        int issueId,
        CreateIssueCommentRequest request,
        string userId)
    {
        throw new NotImplementedException();
    }

    public Task<IssueOperationResult> UpdateIssueAsync(
        int projectId,
        int id,
        UpdateIssueRequest request,
        string userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateIssueCommentAsync(
        int projectId,
        int issueId,
        int commentId,
        UpdateIssueCommentRequest request,
        string userId
    )
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteIssueAsync(
        int projectId,
        int id,
        string userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteIssueCommentAsync(
        int projectId,
        int issueId,
        int commentId,
        string userId)
    {
        throw new NotImplementedException();
    }
}