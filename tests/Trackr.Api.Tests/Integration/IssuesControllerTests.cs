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
        var userId = await AuthenticationHelper.AuthenticateAsync(client);

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
        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);
        await SeedProjectAsync(factory, userId);

        var response = await client.GetAsync("/api/projects/1/issues");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetIssues_ReturnsEmptyPagedResponse_WhenProjectHasNoIssues()
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);
        await SeedProjectAsync(factory, userId);

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
        var userId = await AuthenticationHelper.AuthenticateAsync(client);

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
        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);
        await SeedProjectAsync(factory, userId);

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
        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);
        await SeedProjectAsync(factory, userId);

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
        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);
        await SeedProjectAsync(factory, userId);

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
        var userId = await AuthenticationHelper.AuthenticateAsync(client);

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

    [Fact]
    public async Task CreateIssue_ReturnsBadRequest_WhenAssigneeIsAnotherUser()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();

        var ownerId =
            await AuthenticationHelper.AuthenticateAsync(
                ownerClient,
                "owner@trackr.com");

        await SeedProjectAsync(
            factory,
            ownerId);

        var otherClient = factory.CreateClient();

        var otherUserId =
            await AuthenticationHelper.AuthenticateAsync(
                otherClient,
                "other@trackr.com");

        var request = new CreateIssueRequest
        {
            Title = "Invalid assignment",
            Description = "Should be rejected",
            Priority = IssuePriority.Medium,
            AssigneeId = otherUserId
        };

        var response = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/issues",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(
                JsonOptions);

        Assert.NotNull(problem);

        Assert.Equal(400, problem.Status);
        Assert.Equal("Invalid assignee", problem.Title);

        Assert.Equal(
            "The issue can only be assigned to the current user.",
            problem.Detail);
    }

    [Fact]
    public async Task CreateIssue_ReturnsAssignee_WhenAssignedToCurrentUser()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        var userId =
            await AuthenticationHelper.AuthenticateAsync(
                client,
                "assigned@trackr.com");

        await SeedProjectAsync(
            factory,
            userId);

        var request = new CreateIssueRequest
        {
            Title = "Assigned issue",
            Description = "Assigned to myself",
            Priority = IssuePriority.High,
            AssigneeId = userId
        };

        var response = await client.PostAsJsonAsync(
            "/api/projects/1/issues",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var issue = await response.Content
            .ReadFromJsonAsync<IssueResponse>(
                JsonOptions);

        Assert.NotNull(issue);
        Assert.Equal(userId, issue.AssigneeId);
    }

    [Fact]
    public async Task UpdateIssue_ReturnsBadRequest_WhenAssigneeIsAnotherUser()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();

        var ownerId = await AuthenticationHelper.AuthenticateAsync(ownerClient, "owner-update@trackr.com");

        await SeedProjectAsync(factory, ownerId);

        var otherClient = factory.CreateClient();

        var otherUserId = await AuthenticationHelper.AuthenticateAsync(otherClient, "other-update@trackr.com");

        var createRequest = new CreateIssueRequest
        {
            Title = "Original issue",
            Description = "Original description",
            Priority = IssuePriority.Medium
        };

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/issues",
            createRequest
        );

        createResponse.EnsureSuccessStatusCode();

        var createdIssue = await createResponse.Content
            .ReadFromJsonAsync<IssueResponse>(JsonOptions);

        Assert.NotNull(createdIssue);

        var updateRequest = new UpdateIssueRequest
        {
            Title = "Should not update",
            Description = "Should not persist",
            Status = IssueStatus.Done,
            Priority = IssuePriority.Critical,
            AssigneeId = otherUserId
        };

        var response = await ownerClient.PutAsJsonAsync(
            $"/api/projects/1/issues/{createdIssue.Id}",
            updateRequest
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Invalid assignee", problem.Title);
        Assert.Equal("The issue can only be assigned to the current user.", problem.Detail);
    }

    [Fact]
    public async Task CreateIssueComment_ReturnsCreated_WhenRequestIsValid()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        var userId = await AuthenticationHelper.AuthenticateAsync(
            client,
            "comment-author@trackr.com");

        await SeedIssueAsync(factory, userId);

        var request = new CreateIssueCommentRequest
        {
            Content = "Created through the API"
        };

        var response = await client.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var comment = await response.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(comment);
        Assert.Equal("Created through the API", comment.Content);
        Assert.Equal(1, comment.IssueId);
        Assert.Equal(userId, comment.AuthorId);
        Assert.NotNull(response.Headers.Location);

        Assert.Contains(
            $"/api/projects/1/issues/1/comments/{comment.Id}",
            response.Headers.Location.ToString());
    }

    [Fact]
    public async Task CreateIssueComment_ReturnsNotFound_WhenIssueBelongsToAnotherUser()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();

        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "comment-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var otherClient = factory.CreateClient();

        await AuthenticationHelper.AuthenticateAsync(
            otherClient,
            "comment-other@trackr.com");

        var request = new CreateIssueCommentRequest
        {
            Content = "Unauthorized comment"
        };

        var response = await otherClient.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Issue not found", problem.Title);
        Assert.Equal("Issue with id 1 was not found.", problem.Detail);
    }

    [Fact]
    public async Task GetIssueComments_ReturnsComments_WhenIssueBelongsToUser()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);

        await SeedIssueAsync(factory, userId);

        await client.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest { Content = "First comment" });

        await client.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest { Content = "Second comment" });

        var response = await client.GetAsync(
            "/api/projects/1/issues/1/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var comments = await response.Content
            .ReadFromJsonAsync<List<IssueCommentResponse>>(JsonOptions);

        Assert.NotNull(comments);
        Assert.Collection(
            comments,
            comment => Assert.Equal("First comment", comment.Content),
            comment => Assert.Equal("Second comment", comment.Content));
    }

    [Fact]
    public async Task GetIssueComments_ReturnsNotFound_WhenIssueBelongsToAnotherUser()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "comments-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var otherClient = factory.CreateClient();

        await AuthenticationHelper.AuthenticateAsync(
            otherClient,
            "comments-reader@trackr.com");

        var response = await otherClient.GetAsync(
            "/api/projects/1/issues/1/comments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetIssueCommentById_ReturnsComment_WhenIssueBelongsToUser()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        var userId = await AuthenticationHelper.AuthenticateAsync(
            client,
            "comment-reader@trackr.com");

        await SeedIssueAsync(factory, userId);

        var createResponse = await client.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest
            {
                Content = "Comment to retrieve"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var response = await client.GetAsync(
            createResponse.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var comment = await response.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(comment);
        Assert.Equal("Comment to retrieve", comment.Content);
        Assert.Equal(1, comment.IssueId);
        Assert.Equal(userId, comment.AuthorId);
    }

    [Fact]
    public async Task GetIssueCommentById_ReturnsNotFound_WhenIssueBelongsToAnotherUser()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();

        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "comment-lookup-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest
            {
                Content = "Private comment"
            });

        var createdComment = await createResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(createdComment);

        var otherClient = factory.CreateClient();

        await AuthenticationHelper.AuthenticateAsync(
            otherClient,
            "comment-lookup-other@trackr.com");

        var response = await otherClient.GetAsync(
            $"/api/projects/1/issues/1/comments/{createdComment.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(problem);
        Assert.Equal("Comment not found", problem.Title);
    }

    [Fact]
    public async Task UpdateIssueComment_ReturnsNoContent_WhenUserIsAuthor()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);

        await SeedIssueAsync(factory, userId);

        var createResponse = await client.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest
            {
                Content = "Original content"
            });

        var createdComment = await createResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(createdComment);

        var response = await client.PutAsJsonAsync(
            $"/api/projects/1/issues/1/comments/{createdComment.Id}",
            new UpdateIssueCommentRequest
            {
                Content = "Updated content"
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/projects/1/issues/1/comments/{createdComment.Id}");

        var updatedComment = await getResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(updatedComment);
        Assert.Equal("Updated content", updatedComment.Content);
        Assert.True(updatedComment.UpdatedAt >= createdComment.UpdatedAt);
    }

    [Fact]
    public async Task UpdateIssueComment_ReturnsNotFound_WhenIssueBelongsToAnotherUser()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "comment-update-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest { Content = "Private comment" });

        var comment = await createResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(comment);

        var otherClient = factory.CreateClient();

        await AuthenticationHelper.AuthenticateAsync(
            otherClient,
            "comment-update-other@trackr.com");

        var response = await otherClient.PutAsJsonAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}",
            new UpdateIssueCommentRequest
            {
                Content = "Unauthorized update"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task SeedProjectAsync(TrackrApiFactory factory, string userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrackrDbContext>();

        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Integration Test Project",
            Description = "Project created for integration tests",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedIssueAsync(
    TrackrApiFactory factory,
    string userId)
    {
        await SeedProjectAsync(factory, userId);

        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TrackrDbContext>();

        dbContext.Issues.Add(new Issue
        {
            Id = 1,
            Title = "Integration test issue",
            Description = "Issue used to test comments",
            Status = IssueStatus.Backlog,
            Priority = IssuePriority.Medium,
            ProjectId = 1,
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