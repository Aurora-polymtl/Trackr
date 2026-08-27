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

    public IssuePriority Priority { get; set; } = IssuePriority.Medium;
}