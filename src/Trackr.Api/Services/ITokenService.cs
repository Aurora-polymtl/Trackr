using Trackr.Api.Models;

namespace Trackr.Api.Services;

public interface ITokenService
{
    string CreateToken(ApplicationUser user);
}