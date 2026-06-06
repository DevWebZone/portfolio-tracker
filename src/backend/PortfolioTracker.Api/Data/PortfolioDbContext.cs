using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Models;

namespace PortfolioTracker.Api.Data;

public class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<MarketPrice> MarketPrices => Set<MarketPrice>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asset>().HasIndex(asset => asset.Symbol).IsUnique();
        modelBuilder.Entity<Asset>().Property(asset => asset.Symbol).HasMaxLength(20);
        modelBuilder.Entity<Asset>().Property(asset => asset.Name).HasMaxLength(100);
        modelBuilder.Entity<Asset>().Property(asset => asset.AssetType).HasConversion<string>();

        modelBuilder.Entity<Position>().HasIndex(position => new { position.UserId, position.Symbol }).IsUnique();
        modelBuilder.Entity<Position>().Property(position => position.UserId).HasMaxLength(100);
        modelBuilder.Entity<Position>().Property(position => position.Symbol).HasMaxLength(20);

        modelBuilder.Entity<MarketPrice>().HasKey(price => price.Symbol);
        modelBuilder.Entity<MarketPrice>().Property(price => price.Symbol).HasMaxLength(20);

        modelBuilder.Entity<Alert>().Property(alert => alert.UserId).HasMaxLength(100);
        modelBuilder.Entity<Alert>().Property(alert => alert.Symbol).HasMaxLength(20);
        modelBuilder.Entity<Alert>().Property(alert => alert.Direction).HasMaxLength(8);
        modelBuilder.Entity<Alert>().Property(alert => alert.Severity).HasConversion<string>();

        modelBuilder.Entity<Transaction>().Property(transaction => transaction.UserId).HasMaxLength(100);
        modelBuilder.Entity<Transaction>().Property(transaction => transaction.Symbol).HasMaxLength(20);
        modelBuilder.Entity<Transaction>().Property(transaction => transaction.Side).HasConversion<string>();
    }
}
