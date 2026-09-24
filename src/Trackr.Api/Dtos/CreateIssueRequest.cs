using System.ComponentModel.DataAnnotations;
using Trackr.Api.Models;

namespace Trackr.Api.Dtos;

public class CreateIssueRequest
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [EnumDataType(typeof(IssuePriority))]
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;

    public string? AssigneeId { get; set; }
}