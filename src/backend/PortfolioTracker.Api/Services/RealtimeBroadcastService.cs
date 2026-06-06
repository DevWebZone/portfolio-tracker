using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Hubs;
using PortfolioTracker.Api.Models;

namespace PortfolioTracker.Api.Services;

public class RealtimeBroadcastService(
    IMarketEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    IHubContext<PortfolioHub> hubContext,
    ILogger<RealtimeBroadcastService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var priceUpdated in eventBus.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await hubContext.Clients.All.SendAsync("PriceUpdated", priceUpdated, stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var alertService = scope.ServiceProvider.GetRequiredService<AlertService>();
                var alerts = await alertService.EvaluateAsync(priceUpdated, stoppingToken);
                foreach (var alert in alerts)
                {
                    await hubContext.Clients.All.SendAsync("AlertGenerated", alert, stoppingToken);
                }

                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PortfolioDbContext>>();
                await using var db = await dbFactory.CreateDbContextAsync(stoppingToken);
                var hasPosition = await db.Positions.AnyAsync(
                    position => position.UserId == DemoUser.UserId && position.Symbol == priceUpdated.Symbol,
                    stoppingToken);

                if (hasPosition || alerts.Count > 0)
                {
                    var portfolioService = scope.ServiceProvider.GetRequiredService<PortfolioService>();
                    var snapshot = await portfolioService.GetPortfolioAsync(DemoUser.UserId, stoppingToken);
                    await hubContext.Clients.All.SendAsync("PortfolioUpdated", snapshot, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Realtime broadcast failed.");
            }
        }
    }
}
