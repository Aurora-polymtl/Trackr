using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trackr.Api.Data;
using Trackr.Api.Dtos;
using Trackr.Api.Models;

namespace Trackr.Api.Tests.Integration;

public class IssuesControllerTests
{
    [Fact]
    public async Task GetIssues_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/projects/999/issues");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetIssues_ReturnsOk_WhenProjectExists()
    {
        await using var factory = new TrackrApiFactory();
        await SeedProjectAsync(factory);
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/projects/1/issues");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetIssues_ReturnsEmptyPagedResponse_WhenProjectHasNoIssues()
    {
        await using var factory = new TrackrApiFactory();
        await SeedProjectAsync(factory);
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/projects/1/issues");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<IssueResponse>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task CreateIssue_ReturnsCreated_WhenRequestIsValid()
    {
        await using var factory = new TrackrApiFactory();
        await SeedProjectAsync(factory);
        var client = factory.CreateClient();
        var request = new CreateIssueRequest
        {
            Title = "Integration test issue",
            Description = "Created through HTTP request",
            Priority = IssuePriority.High
        };
        var response = await client.PostAsJsonAsync("/api/projects/1/issues", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdIssue = await response.Content.ReadFromJsonAsync<IssueResponse>(JsonOptions);
        Assert.NotNull(createdIssue);
        Assert.Equal("Integration test issue", createdIssue.Title);
        Assert.Equal(IssueStatus.Backlog, createdIssue.Status);
        Assert.Equal(IssuePriority.High, createdIssue.Priority);
        Assert.Equal(1, createdIssue.ProjectId);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/projects/1/issues/{createdIssue.Id}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task CreateIssue_ReturnsBadRequest_WhenTitleIsMissing()
    {
        await using var factory = new TrackrApiFactory();
        await SeedProjectAsync(factory);
        var client = factory.CreateClient();
        var request = new 
        {
            description = "Missing title",
            priority = "High"
        };
        var response = await client.PostAsJsonAsync("/api/projects/1/issues", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateIssue_ReturnsBadRequest_WhenPriorityIsInvalid()
    {
        await using var factory = new TrackrApiFactory();
        await SeedProjectAsync(factory);
        var client = factory.CreateClient();
        var request = new 
        {
            title = "Invalid priority issue",
            description = "Test",
            priority = "InvalidPriority"
        };
        var response = await client.PostAsJsonAsync("/api/projects/1/issues", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task SeedProjectAsync(TrackrApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrackrDbContext>();
        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Integration Test Project",
            Description = "Project created for integration tests",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    private static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}