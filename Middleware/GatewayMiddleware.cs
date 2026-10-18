namespace gateway.Middleware;

using gateway.Services;

public sealed class GatewayMiddleware(RequestDelegate next, ILogger<GatewayMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext context,
        IJwksService jwksService,
        IJwtValidationService jwtValidationService)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();

            var decodedToken = jwtValidationService.DecodeToken(token);
            if (decodedToken is not null)
            {
                logger.LogInformation("Decoded JWT - Alg: {Alg}, Kid: {Kid}", decodedToken.Header.Alg, decodedToken.Header.Kid);
            }

            var jwks = await jwksService.GetJwksAsync(context.RequestAborted);
            var principal = jwtValidationService.ValidateToken(token, jwks);

            if (principal is not null)
            {
                context.User = principal;
                logger.LogInformation("Token signature verified successfully for subject: {Subject}", principal.FindFirst("sub")?.Value);
            }
            else
            {
                logger.LogWarning("Token signature verification failed.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Invalid token signature.", context.RequestAborted);
                return;
            }
        }

        await next(context);
    }
}
