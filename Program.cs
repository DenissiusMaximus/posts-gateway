using gateway.Middleware;
using gateway.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext());

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IJwksService, JwksService>();
builder.Services.AddScoped<IJwtValidationService, JwtValidationService>();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();


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

app.UseSerilogRequestLogging();

app.UseMiddleware<GatewayMiddleware>();

app.MapReverseProxy();

app.Run();
