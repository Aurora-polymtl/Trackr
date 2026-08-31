using System.ComponentModel.DataAnnotations;
using Trackr.Api.Models;

namespace Trackr.Api.Dtos;

public class IssueQueryParameters
{
    public IssueStatus? Status { get; set; }

    public IssuePriority? Priority { get; set; }

    public string? Search { get; set; }

    public IssueSortBy SortBy { get; set; } = IssueSortBy.CreatedAt;

    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}