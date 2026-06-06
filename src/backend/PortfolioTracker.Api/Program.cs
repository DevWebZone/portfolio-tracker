using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Hubs;
using PortfolioTracker.Api.Models;
using PortfolioTracker.Api.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "portfolio-tracker.db");
builder.Services.AddDbContextFactory<PortfolioDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton<IMarketEventBus, InProcessMarketEventBus>();
builder.Services.AddSingleton<IAlertDeduplicationStore, InMemoryAlertDeduplicationStore>();
builder.Services.AddScoped<PortfolioService>();
builder.Services.AddScoped<MarketService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddHostedService<MarketSimulatorService>();
builder.Services.AddHostedService<RealtimeBroadcastService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PortfolioDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
    await SeedData.EnsureSeededAsync(db);
}

app.MapGet("/api/portfolio/{userId}", async (string userId, PortfolioService service) =>
{
    if (!DemoUser.IsAllowed(userId))
    {
        return Results.NotFound(new { message = "Only demo-user is available in version 1." });
    }

    return Results.Ok(await service.GetPortfolioAsync(userId));
});

app.MapGet("/api/market/prices", async (MarketService service) =>
    Results.Ok(await service.GetPricesAsync()));

app.MapGet("/api/alerts/{userId}", async (string userId, AlertService service) =>
{
    if (!DemoUser.IsAllowed(userId))
    {
        return Results.NotFound(new { message = "Only demo-user is available in version 1." });
    }

    return Results.Ok(await service.GetAlertsAsync(userId));
});

app.MapHub<PortfolioHub>("/hubs/portfolio");

app.Run();

public partial class Program;
