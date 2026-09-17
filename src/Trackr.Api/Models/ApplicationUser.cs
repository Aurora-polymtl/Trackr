using Microsoft.AspNetCore.Identity;

namespace Trackr.Api.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<IssueComment> IssueComments { get; set; } = new List<IssueComment>();
}