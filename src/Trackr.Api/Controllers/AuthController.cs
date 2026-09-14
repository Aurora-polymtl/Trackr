using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Trackr.Api.Dtos;
using Trackr.Api.Models;
using Trackr.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace Trackr.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService
) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group
                .Select(error => error.Description)
                .ToArray());

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            };

            return BadRequest(problemDetails);
        }

        var response = new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The email or password is incorrect."
            );
        }

        var passwordIsValid = await userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordIsValid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The email or password is incorrect."
            );
        }

        var token = tokenService.CreateToken(user);

        var response = new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            AccessToken = token
        };

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            throw new InvalidOperationException("Authenticated user has no identifier.");
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Unauthorized();
        }

        var response = new CurrentUserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty
        };

        return Ok(response);
    }
}