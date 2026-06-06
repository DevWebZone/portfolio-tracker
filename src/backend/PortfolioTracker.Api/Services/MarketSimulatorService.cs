using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Models;

namespace PortfolioTracker.Api.Services;

public class MarketSimulatorService(
    IDbContextFactory<PortfolioDbContext> dbFactory,
    IMarketEventBus eventBus,
    ILogger<MarketSimulatorService> logger) : BackgroundService
{
    private readonly Random _random = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Market simulator tick failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var prices = await db.MarketPrices.ToListAsync(cancellationToken);

        foreach (var price in prices)
        {
            var volatility = price.Symbol is "BTC" or "ETH" ? 0.035m : 0.018m;
            var movement = ((decimal)_random.NextDouble() * 2 - 1) * volatility;
            var nextPrice = Math.Max(0.01m, price.CurrentPrice * (1 + movement));
            price.CurrentPrice = Math.Round(nextPrice, 4);
            price.UpdatedAt = DateTime.UtcNow;

            var movePercent = price.OpeningPrice == 0
                ? 0
                : Math.Round((price.CurrentPrice - price.OpeningPrice) / price.OpeningPrice * 100, 2);

            await eventBus.PublishAsync(
                new PriceUpdatedEvent(price.Symbol, price.CurrentPrice, price.OpeningPrice, movePercent, price.UpdatedAt),
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
