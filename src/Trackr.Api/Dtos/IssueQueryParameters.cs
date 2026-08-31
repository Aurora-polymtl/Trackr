using Trackr.Api.Models;

namespace Trackr.Api.Dtos;

public class IssueQueryParameters
{
    public IssueStatus? Status { get; set; }
    public IssuePriority? Priority { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
}