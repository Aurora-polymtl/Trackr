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

    [Theory]
    [InlineData(IssuePriority.Low, 1)]
    [InlineData(IssuePriority.Medium, 1)]
    [InlineData(IssuePriority.High, 1)]
    [InlineData(IssuePriority.Critical, 1)]
    public async Task GetIssuesByProjectAsync_FiltersByPriority(IssuePriority priority, int expectedCount)
    {
        await using var dbContext = CreateDbContext();
        await SeedIssuesAsync(dbContext);
        var service = new IssueService(dbContext);
        var queryParameters = new IssueQueryParameters { Priority = priority };
        var result = await service.GetIssuesByProjectAsync(1, queryParameters);
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.All(result.Items, issue => Assert.Equal(priority, issue.Priority));
    }

    [Theory]
    [InlineData(IssueStatus.InProgress, 2)]
    [InlineData(IssueStatus.Todo, 1)]
    [InlineData(IssueStatus.Backlog, 1)]
    [InlineData(IssueStatus.Done, 0)]
    public async Task GetIssuesByProjectAsync_FiltersByStatus(IssueStatus status, int expectedCount)
    {
        await using var dbContext = CreateDbContext();
        await SeedIssuesAsync(dbContext);
        var service = new IssueService(dbContext);
        var queryParameters = new IssueQueryParameters { Status = status };
        var result = await service.GetIssuesByProjectAsync(1, queryParameters);
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.All(result.Items, issue => Assert.Equal(status, issue.Status));
    }

    [Theory]
    [InlineData("authentication", 2)]
    [InlineData("dashboard", 1)]
    [InlineData("production", 1)]
    public async Task GetIssuesByProjectAsync_SearchesTitleAndDescription(string search, int expectedCount)
    {
        await using var dbContext = CreateDbContext();
        await SeedIssuesAsync(dbContext);
        var service = new IssueService(dbContext);
        var queryParameters = new IssueQueryParameters { Search = search };
        var result = await service.GetIssuesByProjectAsync(1, queryParameters);
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.TotalCount);
    }

    [Fact]
    public async Task GetIssuesByProjectAsync_CombineFilters()
    {
        await using var dbContext = CreateDbContext();
        await SeedIssuesAsync(dbContext);
        var service = new IssueService(dbContext);
        var queryParameters = new IssueQueryParameters
        {
            Status = IssueStatus.InProgress,
            Priority = IssuePriority.High,
        };
        var result = await service.GetIssuesByProjectAsync(1, queryParameters);
        Assert.NotNull(result);
        
        var issue = Assert.Single(result.Items);
        Assert.Equal("Fix authentication", issue.Title);
    }

    [Fact]
    public async Task GetIssuesByProjectAsync_SortsPriorityAscending()
    {
        await using var dbContext = CreateDbContext();
        await SeedIssuesAsync(dbContext);
        var service = new IssueService(dbContext);
        var queryParameters = new IssueQueryParameters
        {
            SortBy = IssueSortBy.Priority,
            SortDirection = SortDirection.Asc
        };
        var result = await service.GetIssuesByProjectAsync(1, queryParameters);
        Assert.NotNull(result);

        var priorities = result.Items.Select(issue => issue.Priority).ToList();
        Assert.Equal(
            [
                IssuePriority.Low,
                IssuePriority.Medium,
                IssuePriority.High,
                IssuePriority.Critical
            ],
            priorities
        );
    }

    [Fact]
    public async Task GetIssuesByProjectAsync_SortsPriorityDescending()
    {
        await using var dbContext = CreateDbContext();
        await SeedIssuesAsync(dbContext);
        var service = new IssueService(dbContext);
        var queryParameters = new IssueQueryParameters
        {
            SortBy = IssueSortBy.Priority,
            SortDirection = SortDirection.Desc
        };
        var result = await service.GetIssuesByProjectAsync(1, queryParameters);
        Assert.NotNull(result);

        var priorities = result.Items.Select(issue => issue.Priority).ToList();
        Assert.Equal(
            [
                IssuePriority.Critical,
                IssuePriority.High,
                IssuePriority.Medium,
                IssuePriority.Low
            ],
            priorities
        );
    }

    private static async Task SeedIssuesAsync(TrackrDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Test Project",
            Description = "Project for testing",
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.Issues.AddRange(
            new Issue
            {
                Title = "Fix authentication",
                Description = "Correct the login flow",
                Status = IssueStatus.InProgress,
                Priority = IssuePriority.High,
                ProjectId = 1,
                CreatedAt = now.AddMinutes(-4),
                UpdatedAt = now.AddMinutes(-4)
            },
            new Issue
            {
                Title = "Create dashboard",
                Description = "Add the project overview",
                Status = IssueStatus.Todo,
                Priority = IssuePriority.Medium,
                ProjectId = 1,
                CreatedAt = now.AddMinutes(-3),
                UpdatedAt = now.AddMinutes(-3)
            },
            new Issue
            {
                Title = "Improve registration",
                Description = "Improve authentication validation",
                Status = IssueStatus.InProgress,
                Priority = IssuePriority.Low,
                ProjectId = 1,
                CreatedAt = now.AddMinutes(-2),
                UpdatedAt = now.AddMinutes(-2)
            },
            new Issue
            {
                Title = "Critical production bug",
                Description = "Fix an important production issue",
                Status = IssueStatus.Backlog,
                Priority = IssuePriority.Critical,
                ProjectId = 1,
                CreatedAt = now.AddMinutes(-1),
                UpdatedAt = now.AddMinutes(-1)
            }
        );
        await dbContext.SaveChangesAsync();
    }
}