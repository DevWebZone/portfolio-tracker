using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Models;
using PortfolioTracker.Api.Services;

namespace PortfolioTracker.Tests;

public class PortfolioServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _factory;

    public PortfolioServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseSqlite(_connection)
            .Options;

        _factory = new TestDbContextFactory(options);
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
        SeedData.EnsureSeededAsync(db).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Buy_creates_new_position_and_records_transaction()
    {
        var service = new PortfolioService(_factory);

        var result = await service.BuyAsync(new TradeRequest(DemoUser.UserId, "AAPL", 5, 200));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(TransactionSide.Buy, result.Value.Transaction.Side);
        Assert.Contains(result.Value.Portfolio.Holdings, holding => holding.Symbol == "AAPL" && holding.Quantity == 30);
    }

    [Fact]
    public async Task Sell_reduces_position_and_calculates_realized_pnl()
    {
        var service = new PortfolioService(_factory);

        var result = await service.SellAsync(new TradeRequest(DemoUser.UserId, "AAPL", 5, 200));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(138.25m, result.Value.Transaction.RealizedPnL);
        Assert.Contains(result.Value.Portfolio.Holdings, holding => holding.Symbol == "AAPL" && holding.Quantity == 20);
    }

    [Fact]
    public async Task Sell_rejects_oversell()
    {
        var service = new PortfolioService(_factory);

        var result = await service.SellAsync(new TradeRequest(DemoUser.UserId, "ETH", 50, 3500));

        Assert.False(result.IsSuccess);
        Assert.Equal("Sell quantity exceeds the current position.", result.Error);
    }

    [Fact]
    public async Task Snapshot_calculates_unrealized_pnl_and_exposure()
    {
        var service = new PortfolioService(_factory);

        var snapshot = await service.GetPortfolioAsync(DemoUser.UserId);

        Assert.True(snapshot.TotalValue > 0);
        Assert.True(snapshot.UnrealizedPnL > 0);
        Assert.InRange(snapshot.Holdings.Sum(holding => holding.ExposurePercent), 99.9m, 100.1m);
    }

    [Fact]
    public async Task Alert_service_triggers_threshold_once_per_day()
    {
        var service = new AlertService(_factory, new InMemoryAlertDeduplicationStore());
        var price = new PriceUpdatedEvent("AAPL", 201, 190, 5.79m, DateTime.UtcNow);

        var first = await service.EvaluateAsync(price);
        var second = await service.EvaluateAsync(price);

        Assert.Single(first);
        Assert.Empty(second);
        Assert.Equal(AlertSeverity.Warning, first[0].Severity);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<PortfolioDbContext> options) : IDbContextFactory<PortfolioDbContext>
    {
        public PortfolioDbContext CreateDbContext() => new(options);
    }
}
