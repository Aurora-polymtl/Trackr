namespace Trackr.Api.Dtos;

public class ProjectMemberResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}