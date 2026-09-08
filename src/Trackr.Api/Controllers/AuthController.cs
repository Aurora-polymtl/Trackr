using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Trackr.Api.Dtos;
using Trackr.Api.Models;

namespace Trackr.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager
) : ControllerBase
{
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
}