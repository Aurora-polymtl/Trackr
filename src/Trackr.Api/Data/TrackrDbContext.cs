using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Trackr.Api.Models;

namespace Trackr.Api.Data;

public class TrackrDbContext : IdentityDbContext<ApplicationUser>
{
    public TrackrDbContext(DbContextOptions<TrackrDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueComment> IssueComments => Set<IssueComment>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrackrDbContext).Assembly);
    }
}