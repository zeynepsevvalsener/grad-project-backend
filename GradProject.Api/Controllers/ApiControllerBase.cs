using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers;

/// <summary>
/// Shared base for all authenticated controllers.
/// Provides <see cref="GetUserIdOrThrow"/> which reads the JWT <c>sub</c> claim
/// and throws <see cref="UnauthorizedAccessException"/> on invalid tokens.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int GetUserIdOrThrow()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException("Invalid token.");

        return userId;
    }
}
