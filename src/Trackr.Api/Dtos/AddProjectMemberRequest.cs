using System.ComponentModel.DataAnnotations;

namespace Trackr.Api.Dtos;

public class AddProjectMemberRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}