using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Trackr.Api.Data;
using Trackr.Api.Models;
using Trackr.Api.Dtos;

namespace Trackr.Api.Tests.Integration;

public class ProjectsControllerTests
{
    [Fact]
    public async Task GetProject_ReturnsProblemDetails_WhenProjectDoesNotExist()
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);

        var response = await client.GetAsync("/api/projects/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(problem);

        Assert.Equal(404, problem.Status);
        Assert.Equal("Project not found", problem.Title);
        Assert.Equal("Project with id 999 was not found.", problem.Detail);
    }

    [Fact]
    public async Task GetProject_ReturnsOk_WhenProjectExists()
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);
        await SeedProjectAsync(factory, userId);

        var response = await client.GetAsync("/api/projects/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_ReturnsUnauthorized_WhenTokenIsInvalid()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "invalid-token"
        );

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_AllowsAuthenticatedUser()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        var userId = await AuthenticationHelper.AuthenticateAsync(client);

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_ReturnsNotFound_WhenProjectBelongsToAnotherUser()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(ownerClient, "owner@trackr.com");

        await SeedProjectAsync(factory, ownerId);

        var otherClient = factory.CreateClient();
        await AuthenticationHelper.AuthenticateAsync(otherClient, "other@trackr.com");

        var response = await otherClient.GetAsync("/api/projects/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlyCurrentUsersProjects()
    {
        await using var factory = new TrackrApiFactory();

        var firstClient = factory.CreateClient();

        var firstUserId =
            await AuthenticationHelper.AuthenticateAsync(
                firstClient,
                "first@trackr.com");

        await SeedProjectAsync(
            factory,
            firstUserId);

        var secondClient = factory.CreateClient();

        await AuthenticationHelper.AuthenticateAsync(
            secondClient,
            "second@trackr.com");

        var response = await secondClient.GetAsync(
            "/api/projects");

        response.EnsureSuccessStatusCode();

        var projects = await response.Content
            .ReadFromJsonAsync<List<ProjectResponse>>();

        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task UpdateProject_ReturnsProblemDetails_WhenProjectDoesNotExist()
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);

        var request = new
        {
            Name = "Updated Project",
            Description = "Updated Description"
        };

        var response = await client.PutAsJsonAsync("/api/projects/999", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(problem);

        Assert.Equal(404, problem.Status);
        Assert.Equal("Project not found", problem.Title);
    }

    [Fact]
    public async Task DeleteProject_ReturnsProblemDetails_WhenProjectDoesNotExist()
    {
        await using var factory = new TrackrApiFactory();
        var client = factory.CreateClient();
        var userId = await AuthenticationHelper.AuthenticateAsync(client);

        var response = await client.DeleteAsync("/api/projects/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(problem);

        Assert.Equal(404, problem.Status);
        Assert.Equal("Project not found", problem.Title);
    }

    [Fact]
    public async Task AddProjectMember_ReturnsCreated_WhenRequestIsValid()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "member-owner@trackr.com");

        await SeedProjectAsync(factory, ownerId);

        var memberClient = factory.CreateClient();
        var memberId = await AuthenticationHelper.AuthenticateAsync(
            memberClient,
            "new-member@trackr.com");

        var response = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/members",
            new AddProjectMemberRequest
            {
                Email = "new-member@trackr.com"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var member = await response.Content
            .ReadFromJsonAsync<ProjectMemberResponse>(JsonOptions);

        Assert.NotNull(member);
        Assert.Equal(memberId, member.UserId);
        Assert.Equal("new-member@trackr.com", member.Email);

        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TrackrDbContext>();

        var persistedMember = await dbContext.ProjectMembers.SingleAsync();

        Assert.Equal(1, persistedMember.ProjectId);
        Assert.Equal(memberId, persistedMember.UserId);
    }

    [Fact]
    public async Task AddProjectMember_ReturnsNotFound_WhenUserIsNotOwner()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "project-owner@trackr.com");

        await SeedProjectAsync(factory, ownerId);

        var otherClient = factory.CreateClient();

        await AuthenticationHelper.AuthenticateAsync(
            otherClient,
            "non-owner@trackr.com");

        var response = await otherClient.PostAsJsonAsync(
            "/api/projects/1/members",
            new AddProjectMemberRequest
            {
                Email = "project-owner@trackr.com"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddProjectMember_ReturnsConflict_WhenMemberAlreadyExists()
    {
        await using var factory = new TrackrApiFactory();

        var ownerClient = factory.CreateClient();
        var ownerId = await AuthenticationHelper.AuthenticateAsync(
            ownerClient,
            "duplicate-owner@trackr.com");

        await SeedProjectAsync(factory, ownerId);

        var memberClient = factory.CreateClient();

        await AuthenticationHelper.AuthenticateAsync(
            memberClient,
            "duplicate-member@trackr.com");

        var request = new AddProjectMemberRequest
        {
            Email = "duplicate-member@trackr.com"
        };

        var firstResponse = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/members",
            request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await ownerClient.PostAsJsonAsync(
            "/api/projects/1/members",
            request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    private readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task SeedProjectAsync(TrackrApiFactory factory, string userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrackrDbContext>();

        var now = DateTime.UtcNow;

        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Integration test project",
            Description = "Project created for tests",
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync();
    }
}