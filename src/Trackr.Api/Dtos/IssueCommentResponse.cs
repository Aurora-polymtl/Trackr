using System.Runtime.CompilerServices;

namespace Trackr.Api.Dtos;

public class IssueCommentResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } 
    public DateTime UpdatedAt { get; set; }
    public int IssueId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
}