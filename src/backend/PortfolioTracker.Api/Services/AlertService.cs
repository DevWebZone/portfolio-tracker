using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Models;

namespace PortfolioTracker.Api.Services;

public class AlertService(
    IDbContextFactory<PortfolioDbContext> dbFactory,
    IAlertDeduplicationStore deduplicationStore)
{
    private static readonly decimal[] Thresholds = [5m, 10m];

    public async Task<IReadOnlyList<AlertGeneratedEvent>> EvaluateAsync(PriceUpdatedEvent price, CancellationToken cancellationToken = default)
    {
        var generated = new List<AlertGeneratedEvent>();

        foreach (var threshold in Thresholds)
        {
            if (price.MovePercent >= threshold)
            {
                generated.Add(ToEvent(price, threshold, "+"));
            }

            if (price.MovePercent <= -threshold)
            {
                generated.Add(ToEvent(price, threshold, "-"));
            }
        }

        if (generated.Count == 0)
        {
            return generated;
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var persisted = new List<AlertGeneratedEvent>();

        foreach (var alert in generated)
        {
            if (!deduplicationStore.TryMark(alert))
            {
                continue;
            }

            db.Alerts.Add(new Alert
            {
                Id = alert.Id,
                UserId = alert.UserId,
                Symbol = alert.Symbol,
                Threshold = alert.Threshold,
                Direction = alert.Direction,
                Severity = alert.Severity,
                TriggeredAt = alert.TriggeredAt
            });
            persisted.Add(alert);
        }

        await db.SaveChangesAsync(cancellationToken);
        return persisted;
    }

    public async Task<IReadOnlyList<AlertDto>> GetAlertsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Alerts
            .Where(alert => alert.UserId == userId)
            .OrderByDescending(alert => alert.TriggeredAt)
            .Take(50)
            .Select(alert => new AlertDto(alert.Id, alert.Symbol, alert.Threshold, alert.Direction, alert.Severity, alert.TriggeredAt))
            .ToListAsync(cancellationToken);
    }

    private static AlertGeneratedEvent ToEvent(PriceUpdatedEvent price, decimal threshold, string direction) =>
        new(
            Guid.NewGuid(),
            DemoUser.UserId,
            price.Symbol,
            threshold,
            direction,
            threshold >= 10 ? AlertSeverity.Critical : AlertSeverity.Warning,
            DateTime.UtcNow);
}
