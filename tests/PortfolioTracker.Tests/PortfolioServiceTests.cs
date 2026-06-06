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
    public async Task Snapshot_returns_seeded_static_holdings()
    {
        var service = new PortfolioService(_factory);

        var snapshot = await service.GetPortfolioAsync(DemoUser.UserId);

        Assert.Equal(4, snapshot.Holdings.Count);
        Assert.Contains(snapshot.Holdings, holding => holding.Symbol == "AAPL" && holding.Quantity == 25);
        Assert.Contains(snapshot.Holdings, holding => holding.Symbol == "MSFT" && holding.Quantity == 12);
        Assert.Contains(snapshot.Holdings, holding => holding.Symbol == "BTC" && holding.Quantity == 0.18m);
        Assert.Contains(snapshot.Holdings, holding => holding.Symbol == "ETH" && holding.Quantity == 1.5m);
    }

    [Fact]
    public async Task Snapshot_has_zero_realized_pnl_for_static_portfolio()
    {
        var service = new PortfolioService(_factory);

        var snapshot = await service.GetPortfolioAsync(DemoUser.UserId);

        Assert.Equal(0, snapshot.RealizedPnL);
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
    public async Task Snapshot_returns_zero_totals_for_empty_portfolio()
    {
        await using var db = _factory.CreateDbContext();
        db.Positions.RemoveRange(db.Positions.Where(position => position.UserId == DemoUser.UserId));
        await db.SaveChangesAsync();

        var service = new PortfolioService(_factory);

        var snapshot = await service.GetPortfolioAsync(DemoUser.UserId);

        Assert.Empty(snapshot.Holdings);
        Assert.Equal(0, snapshot.TotalValue);
        Assert.Equal(0, snapshot.CostValue);
        Assert.Equal(0, snapshot.UnrealizedPnL);
        Assert.Equal(0, snapshot.RealizedPnL);
        Assert.Equal(0, snapshot.PnLPercent);
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

    [Theory]
    [InlineData(10.25, "+", AlertSeverity.Warning, AlertSeverity.Critical, 2)]
    [InlineData(-5.25, "-", AlertSeverity.Warning, null, 1)]
    [InlineData(-10.25, "-", AlertSeverity.Warning, AlertSeverity.Critical, 2)]
    public async Task Alert_service_generates_expected_threshold_alerts(
        decimal movePercent,
        string direction,
        AlertSeverity firstSeverity,
        AlertSeverity? secondSeverity,
        int expectedCount)
    {
        var service = new AlertService(_factory, new InMemoryAlertDeduplicationStore());
        var price = new PriceUpdatedEvent("MSFT", 0, 100, movePercent, DateTime.UtcNow);

        var alerts = await service.EvaluateAsync(price);

        Assert.Equal(expectedCount, alerts.Count);
        Assert.All(alerts, alert => Assert.Equal(direction, alert.Direction));
        Assert.Contains(alerts, alert => alert.Threshold == 5 && alert.Severity == firstSeverity);

        if (secondSeverity is not null)
        {
            Assert.Contains(alerts, alert => alert.Threshold == 10 && alert.Severity == secondSeverity);
        }
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<PortfolioDbContext> options) : IDbContextFactory<PortfolioDbContext>
    {
        public PortfolioDbContext CreateDbContext() => new(options);
    }
}
