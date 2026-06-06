namespace PortfolioTracker.Api.Models;

public record TradeRequest(string UserId, string Symbol, decimal Quantity, decimal Price);

public record MarketPriceDto(string Symbol, decimal CurrentPrice, decimal OpeningPrice, decimal MovePercent, DateTime UpdatedAt);

public record AlertDto(Guid Id, string Symbol, decimal Threshold, string Direction, AlertSeverity Severity, DateTime TriggeredAt);

public record HoldingDto(
    string Symbol,
    string Name,
    AssetType AssetType,
    decimal Quantity,
    decimal AverageBuyPrice,
    decimal CurrentPrice,
    decimal CostValue,
    decimal MarketValue,
    decimal UnrealizedPnL,
    decimal PnLPercent,
    decimal ExposurePercent);

public record PortfolioSnapshotDto(
    string UserId,
    decimal TotalValue,
    decimal CostValue,
    decimal UnrealizedPnL,
    decimal RealizedPnL,
    decimal PnLPercent,
    IReadOnlyList<HoldingDto> Holdings,
    IReadOnlyList<AlertDto> LatestAlerts,
    DateTime UpdatedAt);

public record TradeResultDto(Transaction Transaction, PortfolioSnapshotDto Portfolio);

public record PriceUpdatedEvent(string Symbol, decimal Price, decimal OpeningPrice, decimal MovePercent, DateTime UpdatedAt);

public record AlertGeneratedEvent(Guid Id, string UserId, string Symbol, decimal Threshold, string Direction, AlertSeverity Severity, DateTime TriggeredAt);

public record OperationResult<T>(bool IsSuccess, T? Value, string? Error)
{
    public static OperationResult<T> Success(T value) => new(true, value, null);
    public static OperationResult<T> Failure(string error) => new(false, default, error);
}
