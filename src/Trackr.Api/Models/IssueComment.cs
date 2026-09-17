namespace Trackr.Api.Models;

public class IssueComment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;
}