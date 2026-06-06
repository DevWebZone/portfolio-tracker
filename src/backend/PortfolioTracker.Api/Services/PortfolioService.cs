using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Models;

namespace PortfolioTracker.Api.Services;

public class PortfolioService(IDbContextFactory<PortfolioDbContext> dbFactory)
{
    public async Task<PortfolioSnapshotDto> GetPortfolioAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await BuildSnapshotAsync(db, userId, cancellationToken);
    }

    public async Task<OperationResult<TradeResultDto>> BuyAsync(TradeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateTrade(request);
        if (validation is not null)
        {
            return OperationResult<TradeResultDto>.Failure(validation);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var symbol = request.Symbol.Trim().ToUpperInvariant();
        if (!await db.Assets.AnyAsync(asset => asset.Symbol == symbol, cancellationToken))
        {
            return OperationResult<TradeResultDto>.Failure($"Unknown symbol {symbol}.");
        }

        var position = await db.Positions.SingleOrDefaultAsync(
            item => item.UserId == request.UserId && item.Symbol == symbol,
            cancellationToken);

        if (position is null)
        {
            position = new Position
            {
                UserId = request.UserId,
                Symbol = symbol,
                Quantity = request.Quantity,
                AverageBuyPrice = request.Price,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Positions.Add(position);
        }
        else
        {
            var totalQuantity = position.Quantity + request.Quantity;
            var totalCost = position.Quantity * position.AverageBuyPrice + request.Quantity * request.Price;
            position.Quantity = totalQuantity;
            position.AverageBuyPrice = Math.Round(totalCost / totalQuantity, 4);
            position.UpdatedAt = DateTime.UtcNow;
        }

        var transaction = new Transaction
        {
            UserId = request.UserId,
            Symbol = symbol,
            Side = TransactionSide.Buy,
            Quantity = request.Quantity,
            Price = request.Price,
            RealizedPnL = 0,
            ExecutedAt = DateTime.UtcNow
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);

        var snapshot = await BuildSnapshotAsync(db, request.UserId, cancellationToken);
        return OperationResult<TradeResultDto>.Success(new TradeResultDto(transaction, snapshot));
    }

    public async Task<OperationResult<TradeResultDto>> SellAsync(TradeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateTrade(request);
        if (validation is not null)
        {
            return OperationResult<TradeResultDto>.Failure(validation);
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var position = await db.Positions.SingleOrDefaultAsync(
            item => item.UserId == request.UserId && item.Symbol == symbol,
            cancellationToken);

        if (position is null || position.Quantity < request.Quantity)
        {
            return OperationResult<TradeResultDto>.Failure("Sell quantity exceeds the current position.");
        }

        var realizedPnL = Math.Round((request.Price - position.AverageBuyPrice) * request.Quantity, 2);
        position.Quantity -= request.Quantity;
        position.UpdatedAt = DateTime.UtcNow;

        if (position.Quantity == 0)
        {
            db.Positions.Remove(position);
        }

        var transaction = new Transaction
        {
            UserId = request.UserId,
            Symbol = symbol,
            Side = TransactionSide.Sell,
            Quantity = request.Quantity,
            Price = request.Price,
            RealizedPnL = realizedPnL,
            ExecutedAt = DateTime.UtcNow
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);

        var snapshot = await BuildSnapshotAsync(db, request.UserId, cancellationToken);
        return OperationResult<TradeResultDto>.Success(new TradeResultDto(transaction, snapshot));
    }

    public async Task<PortfolioSnapshotDto> BuildSnapshotAsync(PortfolioDbContext db, string userId, CancellationToken cancellationToken = default)
    {
        var positions = await db.Positions
            .Where(position => position.UserId == userId)
            .OrderBy(position => position.Symbol)
            .ToListAsync(cancellationToken);

        var symbols = positions.Select(position => position.Symbol).ToArray();
        var assets = await db.Assets
            .Where(asset => symbols.Contains(asset.Symbol))
            .ToDictionaryAsync(asset => asset.Symbol, cancellationToken);

        var prices = await db.MarketPrices
            .Where(price => symbols.Contains(price.Symbol))
            .ToDictionaryAsync(price => price.Symbol, cancellationToken);

        var realizedPnL = await db.Transactions
            .Where(transaction => transaction.UserId == userId && transaction.Side == TransactionSide.Sell)
            .SumAsync(transaction => transaction.RealizedPnL, cancellationToken);

        var holdingDrafts = positions.Select(position =>
        {
            prices.TryGetValue(position.Symbol, out var price);
            assets.TryGetValue(position.Symbol, out var asset);
            var currentPrice = price?.CurrentPrice ?? position.AverageBuyPrice;
            var costValue = position.Quantity * position.AverageBuyPrice;
            var marketValue = position.Quantity * currentPrice;
            var unrealized = marketValue - costValue;
            var pnlPercent = costValue == 0 ? 0 : Math.Round(unrealized / costValue * 100, 2);

            return new HoldingDraft(
                position.Symbol,
                asset?.Name ?? position.Symbol,
                asset?.AssetType ?? AssetType.Equity,
                position.Quantity,
                position.AverageBuyPrice,
                currentPrice,
                Math.Round(costValue, 2),
                Math.Round(marketValue, 2),
                Math.Round(unrealized, 2),
                pnlPercent);
        }).ToList();

        var totalValue = holdingDrafts.Sum(holding => holding.MarketValue);
        var cost = holdingDrafts.Sum(holding => holding.CostValue);
        var unrealizedPnL = holdingDrafts.Sum(holding => holding.UnrealizedPnL);
        var pnlPercentTotal = cost == 0 ? 0 : Math.Round(unrealizedPnL / cost * 100, 2);

        var holdings = holdingDrafts.Select(holding => new HoldingDto(
            holding.Symbol,
            holding.Name,
            holding.AssetType,
            holding.Quantity,
            holding.AverageBuyPrice,
            holding.CurrentPrice,
            holding.CostValue,
            holding.MarketValue,
            holding.UnrealizedPnL,
            holding.PnLPercent,
            totalValue == 0 ? 0 : Math.Round(holding.MarketValue / totalValue * 100, 2))).ToList();

        var alerts = await db.Alerts
            .Where(alert => alert.UserId == userId)
            .OrderByDescending(alert => alert.TriggeredAt)
            .Take(10)
            .Select(alert => new AlertDto(alert.Id, alert.Symbol, alert.Threshold, alert.Direction, alert.Severity, alert.TriggeredAt))
            .ToListAsync(cancellationToken);

        return new PortfolioSnapshotDto(
            userId,
            Math.Round(totalValue, 2),
            Math.Round(cost, 2),
            Math.Round(unrealizedPnL, 2),
            Math.Round(realizedPnL, 2),
            pnlPercentTotal,
            holdings,
            alerts,
            DateTime.UtcNow);
    }

    private static string? ValidateTrade(TradeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || !DemoUser.IsAllowed(request.UserId))
        {
            return "Only demo-user is available in the MVP.";
        }

        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            return "Symbol is required.";
        }

        if (request.Quantity <= 0)
        {
            return "Quantity must be greater than zero.";
        }

        if (request.Price <= 0)
        {
            return "Price must be greater than zero.";
        }

        return null;
    }

    private record HoldingDraft(
        string Symbol,
        string Name,
        AssetType AssetType,
        decimal Quantity,
        decimal AverageBuyPrice,
        decimal CurrentPrice,
        decimal CostValue,
        decimal MarketValue,
        decimal UnrealizedPnL,
        decimal PnLPercent);
}
