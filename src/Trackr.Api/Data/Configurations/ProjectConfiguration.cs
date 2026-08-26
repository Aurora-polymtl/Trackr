using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trackr.Api.Models;

namespace Trackr.Api.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(project => project.Id);

        builder.Property(project => project.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(project => project.Description)
            .HasMaxLength(500);

        builder.Property(project => project.CreatedAt)
            .IsRequired();
    }
}