using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Trackr.Api.Data;
using Trackr.Api.Dtos;
using Trackr.Api.Models;

namespace Trackr.Api.Tests.Integration;

public class MyIssuesControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GetAssignedToMe_RequiresAuthentication()
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/issues/assigned-to-me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?status=999")]
    [InlineData("?priority=999")]
    [InlineData("?dueOnOrBefore=not-a-date")]
    public async Task GetAssignedToMe_RejectsInvalidQueryParameters(string query)
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        await AuthenticationHelper.AuthenticateAsync(client);

        var response = await client.GetAsync(
            $"/api/issues/assigned-to-me{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAssignedToMe_PaginatesAccessibleAssignments()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient, "my-issues-owner@trackr.com");

        var memberClient = factory.CreateClient();
        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient, "my-issues-member@trackr.com");

        var otherClient = factory.CreateClient();
        var otherId = await AuthenticationHelper.AuthenticateAsync(
            otherClient, "my-issues-other@trackr.com");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<TrackrDbContext>();
            var now = DateTime.UtcNow;

            db.Projects.AddRange(
                new Project
                {
                    Id = 1,
                    Name = "First",
                    UserId = ownerId,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Project
                {
                    Id = 2,
                    Name = "Second",
                    UserId = ownerId,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Project
                {
                    Id = 3,
                    Name = "Private",
                    UserId = otherId,
                    CreatedAt = now,
                    UpdatedAt = now
                });

            db.ProjectMembers.AddRange(
                new ProjectMember
                {
                    ProjectId = 1,
                    UserId = memberId,
                    AddedAt = now
                },
                new ProjectMember
                {
                    ProjectId = 2,
                    UserId = memberId,
                    AddedAt = now
                });

            db.Issues.AddRange(
                new Issue
                {
                    Id = 1,
                    Title = "First assignment",
                    ProjectId = 1,
                    AssigneeId = memberId,
                    Status = IssueStatus.InProgress,
                    Priority = IssuePriority.High,
                    DueDate = new DateOnly(2026, 10, 10),
                    CreatedAt = now,
                    UpdatedAt = now.AddMinutes(-1)
                },
                new Issue
                {
                    Id = 2,
                    Title = "Second assignment",
                    ProjectId = 2,
                    AssigneeId = memberId,
                    Status = IssueStatus.Done,
                    Priority = IssuePriority.Low,
                    DueDate = new DateOnly(2026, 10, 20),
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Issue
                {
                    Id = 3,
                    Title = "Owner assignment",
                    ProjectId = 1,
                    AssigneeId = ownerId,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Issue
                {
                    Id = 4,
                    Title = "Inaccessible assignment",
                    ProjectId = 3,
                    AssigneeId = memberId,
                    Status = IssueStatus.InProgress,
                    Priority = IssuePriority.High,
                    DueDate = new DateOnly(2026, 10, 1),
                    CreatedAt = now,
                    UpdatedAt = now.AddMinutes(1)
                });

            await db.SaveChangesAsync();
        }

        var firstPage = await memberClient.GetFromJsonAsync<
            PagedResponse<IssueResponse>>(
            "/api/issues/assigned-to-me?page=1&pageSize=1", JsonOptions);
        Assert.NotNull(firstPage);
        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, Assert.Single(firstPage.Items).Id);

        var secondPage = await memberClient.GetFromJsonAsync<
            PagedResponse<IssueResponse>>(
            "/api/issues/assigned-to-me?page=2&pageSize=1", JsonOptions);
        Assert.NotNull(secondPage);
        Assert.Equal(1, Assert.Single(secondPage.Items).Id);

        var ownerPage = await ownerClient.GetFromJsonAsync<
            PagedResponse<IssueResponse>>(
            "/api/issues/assigned-to-me", JsonOptions);
        Assert.NotNull(ownerPage);
        Assert.Equal(3, Assert.Single(ownerPage.Items).Id);

        var byStatus = await memberClient.GetFromJsonAsync<
    PagedResponse<IssueResponse>>(
    "/api/issues/assigned-to-me?status=InProgress",
    JsonOptions);
        Assert.NotNull(byStatus);
        Assert.Equal(1, byStatus.TotalCount);
        Assert.Equal(1, Assert.Single(byStatus.Items).Id);

        var byPriority = await memberClient.GetFromJsonAsync<
            PagedResponse<IssueResponse>>(
            "/api/issues/assigned-to-me?priority=Low",
            JsonOptions);
        Assert.NotNull(byPriority);
        Assert.Equal(1, byPriority.TotalCount);
        Assert.Equal(2, Assert.Single(byPriority.Items).Id);

        var noMatch = await memberClient.GetFromJsonAsync<
            PagedResponse<IssueResponse>>(
            "/api/issues/assigned-to-me?status=InProgress&priority=Low",
            JsonOptions);
        Assert.NotNull(noMatch);
        Assert.Empty(noMatch.Items);
        Assert.Equal(0, noMatch.TotalCount);
        Assert.Equal(0, noMatch.TotalPages);

        var dueSoon = await memberClient.GetFromJsonAsync<
            PagedResponse<IssueResponse>>(
            "/api/issues/assigned-to-me?status=InProgress&dueOnOrBefore=2026-10-10",
            JsonOptions);

        Assert.NotNull(dueSoon);
        Assert.Equal(1, dueSoon.TotalCount);
        var dueIssue = Assert.Single(dueSoon.Items);
        Assert.Equal(1, dueIssue.Id);
        Assert.Equal(new DateOnly(2026, 10, 10), dueIssue.DueDate);
    }
}