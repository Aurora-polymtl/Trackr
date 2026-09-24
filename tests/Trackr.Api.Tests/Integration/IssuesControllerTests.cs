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
            "The issue can only be assigned to the project owner or a project member.",
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
    public async Task CreateIssue_ReturnsAssignee_WhenAssignedToProjectMember()
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

        var memberClient = factory.CreateClient();

        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient,
            "assigned-member@trackr.com");

        await SeedProjectMemberAsync(factory, 1, memberId);

        var request = new CreateIssueRequest
        {
            Title = "Assigned issue",
            Description = "Assigned to myself",
            Priority = IssuePriority.High,
            AssigneeId = memberId
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
        Assert.Equal(memberId, issue.AssigneeId);
    }

    [Fact]
    public async Task GetIssues_FiltersByAssignee_WhenRequesterIsProjectMember()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient, "filter-owner@trackr.com");
        await SeedProjectAsync(factory, ownerId);

        var memberClient = factory.CreateClient();
        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient, "filter-member@trackr.com");
        await SeedProjectMemberAsync(factory, 1, memberId);

        var assigned = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/issues",
            new CreateIssueRequest
            {
                Title = "Assigned issue",
                AssigneeId = memberId
            });
        Assert.Equal(HttpStatusCode.Created, assigned.StatusCode);

        var unassigned = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/issues",
            new CreateIssueRequest { Title = "Unassigned issue" });
        Assert.Equal(HttpStatusCode.Created, unassigned.StatusCode);

        var response = await memberClient.GetAsync(
            $"/api/projects/1/issues?assigneeId={Uri.EscapeDataString(memberId)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content
            .ReadFromJsonAsync<PagedResponse<IssueResponse>>(JsonOptions);

        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);

        var issue = Assert.Single(page.Items);
        Assert.Equal("Assigned issue", issue.Title);
        Assert.Equal(memberId, issue.AssigneeId);
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
        Assert.Equal("The issue can only be assigned to the project owner or a project member.", problem.Detail);
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

    [Fact]
    public async Task DeleteIssueComment_ReturnsNoContent_WhenUserIsAuthor()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);

        await SeedIssueAsync(factory, userId);

        var createResponse = await client.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest
            {
                Content = "Comment to delete"
            });

        var comment = await createResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(comment);

        var response = await client.DeleteAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteIssueComment_ReturnsNotFound_WhenIssueBelongsToAnotherUser()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "comment-delete-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest
            {
                Content = "Private comment"
            });

        var comment = await createResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(comment);

        var otherClient = factory.CreateClient();

        await AuthenticationHelper.AuthenticateAsync(
            otherClient,
            "comment-delete-other@trackr.com");

        var response = await otherClient.DeleteAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var ownerGetResponse = await ownerClient.GetAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.OK, ownerGetResponse.StatusCode);
    }

    [Fact]
    public async Task GetIssues_ReturnsIssues_WhenUserIsProjectMember()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();

        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "issue-read-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var memberClient = factory.CreateClient();

        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient,
            "issue-read-member@trackr.com");

        await SeedProjectMemberAsync(factory, 1, memberId);

        var response = await memberClient.GetAsync(
            "/api/projects/1/issues");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<IssueResponse>>(JsonOptions);

        Assert.NotNull(result);

        var issue = Assert.Single(result.Items);

        Assert.Equal(1, issue.Id);
        Assert.Equal("Integration test issue", issue.Title);
    }

    [Fact]
    public async Task ProjectMember_CanReadIssueAndComments()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();

        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "content-read-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var createCommentResponse = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest
            {
                Content = "Shared comment"
            });

        var createdComment = await createCommentResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(createdComment);

        var memberClient = factory.CreateClient();

        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient,
            "content-read-member@trackr.com");

        await SeedProjectMemberAsync(factory, 1, memberId);

        var issueResponse = await memberClient.GetAsync(
            "/api/projects/1/issues/1");

        Assert.Equal(HttpStatusCode.OK, issueResponse.StatusCode);

        var commentsResponse = await memberClient.GetAsync(
            "/api/projects/1/issues/1/comments");

        Assert.Equal(HttpStatusCode.OK, commentsResponse.StatusCode);

        var comments = await commentsResponse.Content
            .ReadFromJsonAsync<List<IssueCommentResponse>>(JsonOptions);

        Assert.NotNull(comments);

        var comment = Assert.Single(comments);

        Assert.Equal("Shared comment", comment.Content);

        var commentResponse = await memberClient.GetAsync(
            $"/api/projects/1/issues/1/comments/{createdComment.Id}");

        Assert.Equal(HttpStatusCode.OK, commentResponse.StatusCode);
    }

    [Fact]
    public async Task CreateIssue_ReturnsNotFound_WhenUserIsOnlyProjectMember()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();

        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "readonly-owner@trackr.com");

        await SeedProjectAsync(factory, ownerId);

        var memberClient = factory.CreateClient();

        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient,
            "readonly-member@trackr.com");

        await SeedProjectMemberAsync(factory, 1, memberId);

        var response = await memberClient.PostAsJsonAsync(
            "/api/projects/1/issues",
            new CreateIssueRequest
            {
                Title = "Unauthorized issue",
                Description = "Members currently have read-only access",
                Priority = IssuePriority.Medium
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProjectMember_CanManageOwnComment()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();

        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "member-comment-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var memberClient = factory.CreateClient();

        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient,
            "member-comment-author@trackr.com");

        await SeedProjectMemberAsync(factory, 1, memberId);

        var createResponse = await memberClient.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest
            {
                Content = "Original member comment"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var comment = await createResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(comment);
        Assert.Equal(memberId, comment.AuthorId);

        var updateResponse = await memberClient.PutAsJsonAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}",
            new UpdateIssueCommentRequest
            {
                Content = "Updated member comment"
            });

        Assert.Equal(
            HttpStatusCode.NoContent,
            updateResponse.StatusCode);

        var getResponse = await memberClient.GetAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}");

        var updatedComment = await getResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(updatedComment);
        Assert.Equal(
            "Updated member comment",
            updatedComment.Content);

        var deleteResponse = await memberClient.DeleteAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var deletedCommentResponse = await memberClient.GetAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            deletedCommentResponse.StatusCode);
    }

    [Fact]
    public async Task ProjectOwner_CannotModifyOrDeleteMemberComment()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();

        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "protected-comment-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var memberClient = factory.CreateClient();

        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient,
            "protected-comment-member@trackr.com");

        await SeedProjectMemberAsync(factory, 1, memberId);

        var createResponse = await memberClient.PostAsJsonAsync(
            "/api/projects/1/issues/1/comments",
            new CreateIssueCommentRequest
            {
                Content = "Member-owned comment"
            });

        var comment = await createResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(comment);

        var updateResponse = await ownerClient.PutAsJsonAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}",
            new UpdateIssueCommentRequest
            {
                Content = "Owner update attempt"
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            updateResponse.StatusCode);

        var deleteResponse = await ownerClient.DeleteAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            deleteResponse.StatusCode);

        var getResponse = await memberClient.GetAsync(
            $"/api/projects/1/issues/1/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var persistedComment = await getResponse.Content
            .ReadFromJsonAsync<IssueCommentResponse>(JsonOptions);

        Assert.NotNull(persistedComment);
        Assert.Equal(
            "Member-owned comment",
            persistedComment.Content);
    }

    [Fact]
    public async Task UpdateIssueStatus_AllowsAssignee_AndRevokesAccessWhenRemoved()
    {
        await using var factory = new TrackrApiFactory();
        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient, "status-owner@trackr.com");

        await SeedProjectAsync(factory, ownerId);

        var memberClient = factory.CreateClient();
        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient, "status-member@trackr.com");

        await SeedProjectMemberAsync(factory, 1, memberId);

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/issues",
            new CreateIssueRequest
            {
                Title = "Assigned issue",
                Description = "Keep this description",
                Priority = IssuePriority.High,
                AssigneeId = memberId
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content
            .ReadFromJsonAsync<IssueResponse>(JsonOptions);
        Assert.NotNull(created);

        var issueUrl = $"/api/projects/1/issues/{created.Id}";

        var memberUpdate = await memberClient.PutAsJsonAsync(
            $"{issueUrl}/status",
            new UpdateIssueStatusRequest { Status = IssueStatus.InProgress },
            JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, memberUpdate.StatusCode);

        var updated = await ownerClient.GetFromJsonAsync<IssueResponse>(
            issueUrl, JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(IssueStatus.InProgress, updated.Status);
        Assert.Equal("Assigned issue", updated.Title);
        Assert.Equal("Keep this description", updated.Description);
        Assert.Equal(IssuePriority.High, updated.Priority);
        Assert.Equal(memberId, updated.AssigneeId);

        var removal = await ownerClient.DeleteAsync(
            $"/api/projects/1/members/{memberId}");
        Assert.Equal(HttpStatusCode.NoContent, removal.StatusCode);

        var afterRemoval = await ownerClient.GetFromJsonAsync<IssueResponse>(
            issueUrl, JsonOptions);
        Assert.NotNull(afterRemoval);
        Assert.Null(afterRemoval.AssigneeId);

        var formerMemberUpdate = await memberClient.PutAsJsonAsync(
            $"{issueUrl}/status",
            new UpdateIssueStatusRequest { Status = IssueStatus.Done },
            JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, formerMemberUpdate.StatusCode);

        var ownerUpdate = await ownerClient.PutAsJsonAsync(
            $"{issueUrl}/status",
            new UpdateIssueStatusRequest { Status = IssueStatus.Done },
            JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, ownerUpdate.StatusCode);
    }

    [Fact]
    public async Task UpdateIssueStatus_ReturnsNotFound_WhenMemberIsNotAssignee()
    {
        await using var factory = new TrackrApiFactory();
        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient, "unassigned-owner@trackr.com");

        await SeedIssueAsync(factory, ownerId);

        var memberClient = factory.CreateClient();
        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient, "unassigned-member@trackr.com");

        await SeedProjectMemberAsync(factory, 1, memberId);

        var response = await memberClient.PutAsJsonAsync(
            "/api/projects/1/issues/1/status",
            new UpdateIssueStatusRequest { Status = IssueStatus.Done },
            JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var issue = await ownerClient.GetFromJsonAsync<IssueResponse>(
            "/api/projects/1/issues/1", JsonOptions);
        Assert.NotNull(issue);
        Assert.Equal(IssueStatus.Backlog, issue.Status);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"status\":999}")]
    public async Task UpdateIssueStatus_ReturnsBadRequest_WhenStatusIsInvalid(
    string json)
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(client);

        await SeedIssueAsync(factory, ownerId);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PutAsync(
            "/api/projects/1/issues/1/status",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private static async Task SeedProjectMemberAsync(
        TrackrApiFactory factory,
        int projectId,
        string userId)
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TrackrDbContext>();

        dbContext.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            AddedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}