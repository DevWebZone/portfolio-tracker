using System.Threading.Channels;
using PortfolioTracker.Api.Models;

namespace PortfolioTracker.Api.Services;

public interface IMarketEventBus
{
    ChannelReader<PriceUpdatedEvent> Reader { get; }
    ValueTask PublishAsync(PriceUpdatedEvent priceUpdated, CancellationToken cancellationToken = default);
}

public class InProcessMarketEventBus : IMarketEventBus
{
    private readonly Channel<PriceUpdatedEvent> _channel = Channel.CreateUnbounded<PriceUpdatedEvent>();

    public ChannelReader<PriceUpdatedEvent> Reader => _channel.Reader;

    public ValueTask PublishAsync(PriceUpdatedEvent priceUpdated, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(priceUpdated, cancellationToken);
}

public interface IAlertDeduplicationStore
{
    bool TryMark(AlertGeneratedEvent alert);
}

public class InMemoryAlertDeduplicationStore : IAlertDeduplicationStore
{
    private readonly Lock _lock = new();
    private readonly HashSet<string> _keys = [];

    public bool TryMark(AlertGeneratedEvent alert)
    {
        var key = $"{alert.TriggeredAt:yyyyMMdd}:{alert.Symbol}:{alert.Direction}:{alert.Threshold}";
        lock (_lock)
        {
            return _keys.Add(key);
        }
    }
}
