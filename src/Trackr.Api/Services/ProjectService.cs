using Trackr.Api.Models;

namespace Trackr.Api.Services;

public class ProjectService: IProjectService
{
    public IEnumerable<Project> GetProjects()
    {
        return new[]
        {
            new Project
            {
                Id = 1,
                Name = "Trackr",
                Description = "Issue tracker construit avec ASP.NET Core.",
                CreatedAt = new DateTime(2026, 8, 24)
            },
            new Project
            {
                Id = 2,
                Name = "Menulia",
                Description = "Planificateur de repas.",
                CreatedAt = new DateTime(2026, 5, 20)
            }
        };
    }
}