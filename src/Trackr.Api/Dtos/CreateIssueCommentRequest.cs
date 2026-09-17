using System.ComponentModel.DataAnnotations;

namespace Trackr.Api.Dtos;

public class CreateIssueCommentRequest
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}