using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Trackr.Api.Services;
using Trackr.Api.Data;
using Trackr.Api.Dtos;
using Trackr.Api.Models;

namespace Trackr.Api.Tests.Integration;

public class IssuesControllerTests
{
    [Fact]
    public async Task GetIssues_ReturnsProblemDetails_WhenProjectDoesNotExist()
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        await AuthenticationHelper.AuthenticateAsync(client);

        var response = await client.GetAsync("/api/projects/999/issues");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(problem);

        Assert.Equal(404, problem.Status);
        Assert.Equal("Project not found", problem.Title);
        Assert.Equal("Project with id 999 was not found.", problem.Detail);
    }

    [Fact]
    public async Task GetIssues_ReturnsOk_WhenProjectExists()
    {
        await using var factory = new TrackrApiFactory();
        await SeedProjectAsync(factory);
        var client = factory.CreateClient();
        await AuthenticationHelper.AuthenticateAsync(client);

        var response = await client.GetAsync("/api/projects/1/issues");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetIssues_ReturnsEmptyPagedResponse_WhenProjectHasNoIssues()
    {
        await using var factory = new TrackrApiFactory();
        await SeedProjectAsync(factory);
        var client = factory.CreateClient();
        await AuthenticationHelper.AuthenticateAsync(client);

        var response = await client.GetAsync("/api/projects/1/issues");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<IssueResponse>>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetIssues_ReturnsProblemDetails_WhenUnexpectedExceptionOccurs()
    {
        await using var factory = new TrackrApiFactory
        {
            ConfigureTestServices = services =>
            {
                services.RemoveAll<IIssueService>();
                services.AddScoped<IIssueService, ThrowingIssueService>();
            }
        };
        var client = factory.CreateClient();
        await AuthenticationHelper.AuthenticateAsync(client);

        var response = await client.GetAsync("/api/projects/1/issues");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(problem);

        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Equal("An unexpected error occurred while processing the request.", problem.Detail);

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Simulated test exception.", content);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateIssue_ReturnsCreated_WhenRequestIsValid()
    {
        await using var factory = new TrackrApiFactory();
        await SeedProjectAsync(factory);
        var client = factory.CreateClient();
        await AuthenticationHelper.AuthenticateAsync(client);

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
        await AuthenticationHelper.AuthenticateAsync(client);

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
        await AuthenticationHelper.AuthenticateAsync(client);

        var request = new 
        {
            title = "Invalid priority issue",
            description = "Test",
            priority = "InvalidPriority"
        };

        var response = await client.PostAsJsonAsync("/api/projects/1/issues", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateIssue_ReturnsProblemDetails_WhenProjectDoesNotExist()
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        await AuthenticationHelper.AuthenticateAsync(client);
        
        var request = new CreateIssueRequest
        {
            Title = "Test issue",
            Description = "Test",
            Priority = IssuePriority.High
        };

        var response = await client.PostAsJsonAsync("/api/projects/999/issues", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(problem);

        Assert.Equal(404, problem.Status);
        Assert.Equal("Project not found", problem.Title);
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