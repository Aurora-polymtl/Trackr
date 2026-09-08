using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Trackr.Api.Dtos;

namespace Trackr.Api.Tests.Integration;

public class AuthControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Register_ReturnsCreated_WhenRequestIsValid()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        var request = new RegisterRequest
        {
            Email = "test@trackr.com",
            Password = "Trackr123!"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<RegisterResponse>(JsonOptions);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("test@trackr.com", result.Email);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEmailIsInvalid()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "Trackr123!"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenPasswordIsInvalid()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        var request = new RegisterRequest
        {
            Email = "test@trackr.com",
            Password = "abc"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>(
                JsonOptions);

        Assert.NotNull(problem);
        Assert.NotEmpty(problem.Errors);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEmailAlreadyExists()
    {
        await using var factory = new TrackrApiFactory();

        var client = factory.CreateClient();

        var request = new RegisterRequest
        {
            Email = "duplicate@trackr.com",
            Password = "Trackr123!"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode);
    }
}