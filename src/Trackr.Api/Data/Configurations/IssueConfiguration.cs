using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trackr.Api.Models;

namespace Trackr.Api.Data.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.HasKey(issue => issue.Id);

        builder.Property(issue => issue.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(issue => issue.Description)
            .HasMaxLength(2000);

        builder.Property(issue => issue.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(issue => issue.CreatedAt)
            .IsRequired();

        builder.HasOne(issue => issue.Project)
            .WithMany(project => project.Issues)
            .HasForeignKey(issue => issue.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}