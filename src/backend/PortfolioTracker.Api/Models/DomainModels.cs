namespace PortfolioTracker.Api.Models;

public static class DemoUser
{
    public const string UserId = "demo-user";

    public static bool IsAllowed(string userId) =>
        string.Equals(userId, UserId, StringComparison.OrdinalIgnoreCase);
}

public enum AssetType
{
    Equity,
    Crypto
}

public enum AlertSeverity
{
    Warning,
    Critical
}

public class Asset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
}

public class Position
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = DemoUser.UserId;
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class MarketPrice
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal OpeningPrice { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = DemoUser.UserId;
    public string Symbol { get; set; } = string.Empty;
    public decimal Threshold { get; set; }
    public string Direction { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
}
