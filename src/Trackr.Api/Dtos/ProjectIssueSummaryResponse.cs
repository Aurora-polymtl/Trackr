namespace Trackr.Api.Dtos;

public class ProjectIssueSummaryResponse
{
    public int ProjectId { get; set; }
    public int TotalIssues { get; set; }
    public int BacklogCount { get; set; }
    public int TodoCount { get; set; }
    public int InProgressCount { get; set; }
    public int ReviewCount { get; set; }
    public int DoneCount { get; set; }
}