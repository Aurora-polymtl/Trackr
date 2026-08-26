using Trackr.Api.Dtos;
using Trackr.Api.Models;

namespace Trackr.Api.Services;

public interface IIssueService
{
    Task<Issue?> CreateIssueAsync(int projectId, CreateIssueRequest request);
}