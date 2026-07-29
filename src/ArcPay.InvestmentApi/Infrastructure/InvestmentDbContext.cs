using ArcPay.InvestmentApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace ArcPay.InvestmentApi.Infrastructure;

public sealed class InvestmentDbContext(DbContextOptions<InvestmentDbContext> options) : DbContext(options)
{
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Holding> Holdings => Set<Holding>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<FailedCompensation> FailedCompensations => Set<FailedCompensation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var portfolio = modelBuilder.Entity<Portfolio>();
        portfolio.HasIndex(item => item.CustomerNumber).IsUnique();
        portfolio.Property(item => item.CustomerNumber).HasMaxLength(14).IsRequired();
        portfolio.HasMany(item => item.Holdings).WithOne().HasForeignKey(item => item.PortfolioId).OnDelete(DeleteBehavior.Cascade);
        ConfigureBase(portfolio);

        var holding = modelBuilder.Entity<Holding>();
        holding.Property(item => item.Symbol).HasMaxLength(20).IsRequired();
        holding.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        holding.Property(item => item.Quantity).HasPrecision(18, 8);
        holding.Property(item => item.AverageCost).HasPrecision(18, 8);
        holding.HasIndex(item => new { item.PortfolioId, item.Symbol }).IsUnique();
        ConfigureBase(holding);

        var order = modelBuilder.Entity<PurchaseOrder>();
        order.HasIndex(item => item.PurchaseRef).IsUnique();
        order.HasIndex(item => item.RefundRef).IsUnique();
        order.Property(item => item.CustomerNumber).HasMaxLength(14).IsRequired();
        order.Property(item => item.Symbol).HasMaxLength(20).IsRequired();
        order.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        order.Property(item => item.Status).HasMaxLength(32).IsRequired();
        order.Property(item => item.FailureReason).HasMaxLength(500);
        order.Property(item => item.Quantity).HasPrecision(18, 8);
        order.Property(item => item.UnitPrice).HasPrecision(18, 8);
        order.Property(item => item.TotalAmount).HasPrecision(18, 8);
        ConfigureBase(order);

        var failed = modelBuilder.Entity<FailedCompensation>();
        failed.HasIndex(item => item.PaymentTransactionRef).IsUnique();
        failed.Property(item => item.CustomerNumber).HasMaxLength(14).IsRequired();
        failed.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        failed.Property(item => item.Amount).HasPrecision(18, 8);
        failed.Property(item => item.Reason).HasMaxLength(500).IsRequired();
        ConfigureBase(failed);
    }

    private static void ConfigureBase<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : ArcPay.Shared.BaseEntity
    {
        entity.Property(item => item.CreatedBy).HasMaxLength(100).IsRequired();
        entity.Property(item => item.UpdatedBy).HasMaxLength(100).IsRequired();
        entity.Property(item => item.RecordStatus).HasMaxLength(1).IsRequired();
    }
}
