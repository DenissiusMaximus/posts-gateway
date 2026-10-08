namespace gateway.Middleware;

public sealed class GatewayMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory)
{

    public async Task InvokeAsync(HttpContext context)
    {
        var authServiceUrl = context.RequestServices.GetRequiredService<IConfiguration>().GetValue<string>("AuthServiceUrl");
        var client = httpClientFactory.CreateClient();

        using var jwks = await client.GetAsync(
            $"{authServiceUrl}.well-known/jwks",
            context.RequestAborted);

        jwks.EnsureSuccessStatusCode();

        var responseBody = await jwks.Content.ReadAsStringAsync(
            context.RequestAborted);

        Console.WriteLine($"JWKS Response: {responseBody}");

        await next(context);
    }
}
