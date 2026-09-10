using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Trackr.Api.Dtos;

namespace Trackr.Api.Tests.Integration;

public static class AuthenticationHelper
{
    public static async Task<string> AuthenticateAsync(
        HttpClient client,
        string email = "integration@trackr.com",
        string password = "Trackr123!"
    )
    {
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = password
        };

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            registerRequest
        );

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.NotEmpty(loginResult.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult.AccessToken
        );

        return loginResult.UserId;
    }
}