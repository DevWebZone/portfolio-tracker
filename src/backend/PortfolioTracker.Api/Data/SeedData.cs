using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Models;

namespace PortfolioTracker.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(PortfolioDbContext db)
    {
        if (!await db.Assets.AnyAsync())
        {
            db.Assets.AddRange(
                new Asset { Symbol = "AAPL", Name = "Apple Inc.", AssetType = AssetType.Equity },
                new Asset { Symbol = "MSFT", Name = "Microsoft Corp.", AssetType = AssetType.Equity },
                new Asset { Symbol = "BTC", Name = "Bitcoin", AssetType = AssetType.Crypto },
                new Asset { Symbol = "ETH", Name = "Ethereum", AssetType = AssetType.Crypto });
        }

        if (!await db.MarketPrices.AnyAsync())
        {
            db.MarketPrices.AddRange(
                Price("AAPL", 190.25m),
                Price("MSFT", 427.10m),
                Price("BTC", 68250.00m),
                Price("ETH", 3425.50m));
        }

        if (!await db.Positions.AnyAsync(position => position.UserId == DemoUser.UserId))
        {
            db.Positions.AddRange(
                new Position { UserId = DemoUser.UserId, Symbol = "AAPL", Quantity = 25, AverageBuyPrice = 172.35m },
                new Position { UserId = DemoUser.UserId, Symbol = "MSFT", Quantity = 12, AverageBuyPrice = 389.10m },
                new Position { UserId = DemoUser.UserId, Symbol = "BTC", Quantity = 0.18m, AverageBuyPrice = 61100.00m },
                new Position { UserId = DemoUser.UserId, Symbol = "ETH", Quantity = 1.5m, AverageBuyPrice = 2940.00m });
        }

        await db.SaveChangesAsync();
    }

    private static MarketPrice Price(string symbol, decimal price) =>
        new()
        {
            Symbol = symbol,
            CurrentPrice = price,
            OpeningPrice = price,
            UpdatedAt = DateTime.UtcNow
        };
}
