using System.ComponentModel.DataAnnotations;
using Trackr.Api.Models;

namespace Trackr.Api.Dtos;

public class UpdateIssueRequest
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [EnumDataType(typeof(IssueStatus))]
    public IssueStatus Status { get; set; }

    [EnumDataType(typeof(IssuePriority))]
    public IssuePriority Priority { get; set; }

    public string? AssigneeId { get; set; }
}