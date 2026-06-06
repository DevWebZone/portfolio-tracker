using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Models;

namespace PortfolioTracker.Api.Services;

public class MarketService(IDbContextFactory<PortfolioDbContext> dbFactory)
{
    public async Task<IReadOnlyList<MarketPriceDto>> GetPricesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.MarketPrices
            .OrderBy(price => price.Symbol)
            .Select(price => ToDto(price))
            .ToListAsync(cancellationToken);
    }

    public static MarketPriceDto ToDto(MarketPrice price)
    {
        var movePercent = price.OpeningPrice == 0
            ? 0
            : Math.Round((price.CurrentPrice - price.OpeningPrice) / price.OpeningPrice * 100, 2);

        return new MarketPriceDto(price.Symbol, price.CurrentPrice, price.OpeningPrice, movePercent, price.UpdatedAt);
    }
}
