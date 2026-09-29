using System.ComponentModel.DataAnnotations;

namespace Trackr.Api.Dtos;

public class AssignedIssuesQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}