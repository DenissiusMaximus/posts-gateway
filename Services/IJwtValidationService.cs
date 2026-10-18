namespace gateway.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public interface IJwtValidationService
{
    JwtSecurityToken? DecodeToken(string token);

    ClaimsPrincipal? ValidateToken(string token, string jwksJson);

    Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}

