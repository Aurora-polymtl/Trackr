namespace Trackr.Api.Models;

public class Issue
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueStatus Status { get; set; } = IssueStatus.Backlog;
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}