using Trackr.Api.Dtos;
using Trackr.Api.Models;
using Trackr.Api.Services;

namespace Trackr.Api.Tests.Integration;

public class ThrowingIssueService : IIssueService
{
    public Task<PagedResponse<IssueResponse>?> GetIssuesByProjectAsync(
        int projectId,
        IssueQueryParameters queryParameters)
    {
        throw new InvalidOperationException(
            "Simulated test exception.");
    }

    public Task<IssueResponse?> GetIssueByIdAsync(
        int projectId,
        int id)
    {
        throw new NotImplementedException();
    }

    public Task<Issue?> CreateIssueAsync(
        int projectId,
        CreateIssueRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateIssueAsync(
        int projectId,
        int id,
        UpdateIssueRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteIssueAsync(
        int projectId,
        int id)
    {
        throw new NotImplementedException();
    }
}