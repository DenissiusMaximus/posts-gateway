namespace gateway.Services;

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtValidationService(
    IJwksService jwksService,
    ILogger<JwtValidationService> logger) : IJwtValidationService
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<SecurityKey>> KeyCache = new();
    private readonly JwtSecurityTokenHandler _tokenHandler = new() { MapInboundClaims = false };

    public JwtSecurityToken? DecodeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            return _tokenHandler.CanReadToken(token)
                ? _tokenHandler.ReadJwtToken(token)
                : null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to decode JWT token structure.");
            return null;
        }
    }

    public ClaimsPrincipal? ValidateToken(string token, string jwksJson)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(jwksJson))
            return null;

        try
        {
            var signingKeys = GetOrCreateSigningKeys(jwksJson);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            logger.LogInformation("JWT token signature verified successfully for subject: {Subject}",
                principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            logger.LogWarning(ex, "JWT token has expired.");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            logger.LogWarning(ex, "JWT token signature is invalid.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JWT token validation failed.");
            return null;
        }
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var jwksJson = await jwksService.GetJwksAsync(cancellationToken);
        return ValidateToken(token, jwksJson);
    }

    private static IReadOnlyList<SecurityKey> GetOrCreateSigningKeys(string jwksJson)
    {
        return KeyCache.GetOrAdd(jwksJson, json =>
        {
            var jwks = new JsonWebKeySet(json);
            var keys = new List<SecurityKey>();

            foreach (var key in jwks.Keys)
            {
                if (string.Equals(key.Kty, "RSA", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(key.N)
                    && !string.IsNullOrWhiteSpace(key.E))
                {
                    var rsaParams = new RSAParameters
                    {
                        Modulus = Base64UrlEncoder.DecodeBytes(key.N),
                        Exponent = Base64UrlEncoder.DecodeBytes(key.E)
                    };

                    var rsa = RSA.Create();
                    rsa.ImportParameters(rsaParams);

                    keys.Add(new RsaSecurityKey(rsa)
                    {
                        KeyId = key.Kid
                    });
                }
                else
                {
                    keys.Add(key);
                }
            }

            return keys;
        });
    }
}

