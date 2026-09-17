using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trackr.Api.Models;

namespace Trackr.Api.Data.Configurations;

public class IssueCommentConfiguration : IEntityTypeConfiguration<IssueComment>
{
    public void Configure(EntityTypeBuilder<IssueComment> builder)
    {
        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Content)
            .IsRequired()
            .HasMaxLength(2000);
        
        builder.Property(comment => comment.CreatedAt)
            .IsRequired();

        builder.Property(comment => comment.UpdatedAt)
            .IsRequired();

        builder.Property(comment => comment.AuthorId)
            .IsRequired();

        builder.HasOne(comment => comment.Issue)
            .WithMany(issue => issue.Comments)
            .HasForeignKey(comment => comment.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(comment => comment.Author)
            .WithMany(user => user.IssueComments)
            .HasForeignKey(comment => comment.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}