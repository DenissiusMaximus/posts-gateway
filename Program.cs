using gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.Logger.LogInformation("Starting Gateway service [Environment: {Environment}]", app.Environment.EnvironmentName);

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("Gateway service started successfully. Listening for requests.");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogInformation("Gateway service is shutting down...");
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    app.Logger.LogInformation("Gateway service has stopped.");
});

app.UseMiddleware<RequestLoggingMiddleware>();

// app.UseMiddleware<GatewayMiddleware>();  
app.MapReverseProxy();

app.Run();
