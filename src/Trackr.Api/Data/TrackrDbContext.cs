using Microsoft.EntityFrameworkCore;
using Trackr.Api.Models;

namespace Trackr.Api.Data;

public class TrackrDbContext : DbContext
{
    public TrackrDbContext(DbContextOptions<TrackrDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Issue> Issues => Set<Issue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrackrDbContext).Assembly);
    }
}