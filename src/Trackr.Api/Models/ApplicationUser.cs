using Microsoft.AspNetCore.Identity;

namespace Trackr.Api.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}