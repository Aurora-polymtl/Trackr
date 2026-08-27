using Trackr.Api.Models;

namespace Trackr.Api.Dtos;

public class IssueResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueStatus Status { get; set; } = IssueStatus.Backlog;
    public DateTime CreatedAt { get; set; }
    public int ProjectId { get; set; }
}