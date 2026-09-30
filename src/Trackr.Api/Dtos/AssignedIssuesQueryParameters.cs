using System.ComponentModel.DataAnnotations;
using Trackr.Api.Models;

namespace Trackr.Api.Dtos;

public class AssignedIssuesQueryParameters
{
    [EnumDataType(typeof(IssueStatus))]
    public IssueStatus? Status { get; set; }

    [EnumDataType(typeof(IssuePriority))]
    public IssuePriority? Priority { get; set; }

    public DateOnly? DueOnOrBefore { get; set; }
    
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}