namespace gateway.Services;

public interface IJwksService
{
    Task<string> GetJwksAsync(CancellationToken cancellationToken = default);
}

