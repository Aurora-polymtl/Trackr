using System.ComponentModel.DataAnnotations;

namespace Trackr.Api.Dtos;

public class UpdateIssueRequest
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
}