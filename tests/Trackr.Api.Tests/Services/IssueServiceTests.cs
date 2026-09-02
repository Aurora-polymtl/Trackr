using Microsoft.EntityFrameworkCore;
using Trackr.Api.Data;
using Trackr.Api.Models;
using Trackr.Api.Dtos;
using Trackr.Api.Services;

namespace Trackr.Api.Tests.Services;

public class IssueServiceTests
{
    private TrackrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TrackrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TrackrDbContext(options);
    }

    [Fact]
    public async Task GetIssuesByProjectAsync_ReturnsNull_WhenProjectDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = new IssueService(dbContext);
        var queryParameters = new IssueQueryParameters();
        var result = await service.GetIssuesByProjectAsync(999, queryParameters);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIssuesByProjectAsync_ReturnsEmptyPage_WhenProjectHasNoIssues()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Test Project",
            Description = "Project for testing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var service = new IssueService(dbContext);
        var queryParameters = new IssueQueryParameters();
        var result = await service.GetIssuesByProjectAsync(1, queryParameters);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetIssuesByProjectAsync_ReturnsCorrectPage()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Test Project",
            Description = "Project for testing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        dbContext.Issues.AddRange(
            new Issue
            {
                Title = "Issue 1",
                Description = "First issue",
                Priority = IssuePriority.Low,
                Status = IssueStatus.Backlog,
                ProjectId = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new Issue
            {
                Title = "Issue 2",
                Description = "Second issue",
                Priority = IssuePriority.Medium,
                Status = IssueStatus.Backlog,
                ProjectId = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes(-4),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-4)
            },
            new Issue
            {
                Title = "Issue 3",
                Description = "Third issue",
                Priority = IssuePriority.High,
                Status = IssueStatus.Backlog,
                ProjectId = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes(-3),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-3)
            }
        );
        await dbContext.SaveChangesAsync();
        var service = new IssueService(dbContext);
        var queryParameters = new IssueQueryParameters { Page = 2, PageSize = 2 };
        var result = await service.GetIssuesByProjectAsync(1, queryParameters);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);

        var item = Assert.Single(result.Items);
        Assert.Equal("Issue 1", item.Title);
    }
}