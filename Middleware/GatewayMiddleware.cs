namespace gateway.Middleware;

using gateway.Services;

public sealed class GatewayMiddleware(RequestDelegate next)
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
                Console.WriteLine($"Decoded JWT - Alg: {decodedToken.Header.Alg}, Kid: {decodedToken.Header.Kid}");
            }

            var jwks = await jwksService.GetJwksAsync(context.RequestAborted);
            var principal = jwtValidationService.ValidateToken(token, jwks);

            if (principal is not null)
            {
                context.User = principal;
                Console.WriteLine($"Token signature verified successfully for subject: {principal.FindFirst("sub")?.Value}");
            }
            else
            {
                Console.WriteLine("Token signature verification failed.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Invalid token signature.", context.RequestAborted);
                return;
            }
        }
        else
        {
            var jwks = await jwksService.GetJwksAsync(context.RequestAborted);
            Console.WriteLine($"JWKS Response: {jwks}");
        }

        await next(context);
    }
}
