namespace gateway.Services;

public sealed class JwksService(HttpClient httpClient, IConfiguration configuration) : IJwksService
{
    public async Task<string> GetJwksAsync(CancellationToken cancellationToken = default)
    {
        var authServiceUrl = configuration.GetValue<string>("AuthServiceUrl")
            ?? throw new InvalidOperationException("AuthServiceUrl is not configured.");

        var requestUri = $"{authServiceUrl.TrimEnd('/')}/auth/.well-known/jwks";

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

