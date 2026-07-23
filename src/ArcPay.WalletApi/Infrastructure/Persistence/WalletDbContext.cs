using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;
using ArcPay.WalletApi.Domain.Wallets;
using Microsoft.EntityFrameworkCore;

namespace ArcPay.WalletApi.Infrastructure.Persistence;

public sealed class WalletDbContext(DbContextOptions<WalletDbContext> options) : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureWallet(modelBuilder);
        ConfigureTransaction(modelBuilder);
    }

    private static void ConfigureWallet(ModelBuilder modelBuilder)
    {
        var wallet = modelBuilder.Entity<Wallet>();
        wallet.ToTable("Wallets", table =>
            table.HasCheckConstraint("CK_Wallets_Balance_NonNegative", "\"Balance\" >= 0"));
        wallet.HasKey(entity => entity.Id);
        wallet.Property(entity => entity.CustomerNumber)
            .HasConversion(value => value.Value, value => CustomerNumber.FromPersistence(value))
            .HasMaxLength(14)
            .IsRequired();
        wallet.Property(entity => entity.Currency)
            .HasConversion(value => value.Code, value => Currency.FromPersistence(value))
            .HasMaxLength(3)
            .IsRequired();
        wallet.Ignore(entity => entity.Balance);
        wallet.Property<decimal>("_balanceAmount")
            .HasColumnName("Balance")
            .HasPrecision(18, 8)
            .IsRequired();
        wallet.HasIndex(entity => new { entity.CustomerNumber, entity.Currency }).IsUnique();
        ConfigureBaseEntity(wallet);
    }

    private static void ConfigureTransaction(ModelBuilder modelBuilder)
    {
        var transaction = modelBuilder.Entity<Transaction>();
        transaction.ToTable("Transactions", table =>
            table.HasCheckConstraint("CK_Transactions_Amount_Positive", "\"Amount\" > 0"));
        transaction.HasKey(entity => entity.Id);
        transaction.Property(entity => entity.TransactionRef).IsRequired();
        transaction.HasIndex(entity => entity.TransactionRef).IsUnique();
        transaction.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        transaction.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        transaction.Property(entity => entity.Currency)
            .HasConversion(value => value.Code, value => Currency.FromPersistence(value))
            .HasMaxLength(3)
            .IsRequired();
        transaction.Ignore(entity => entity.Amount);
        transaction.Property<decimal>("_amount")
            .HasColumnName("Amount")
            .HasPrecision(18, 8)
            .IsRequired();
        transaction.Property(entity => entity.Description).HasMaxLength(500);
        transaction.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(entity => entity.SenderWalletId)
            .OnDelete(DeleteBehavior.Restrict);
        transaction.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(entity => entity.ReceiverWalletId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureBaseEntity(transaction);
    }

    private static void ConfigureBaseEntity<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : ArcPay.Shared.BaseEntity
    {
        entity.Property(value => value.CreatedBy).HasMaxLength(100).IsRequired();
        entity.Property(value => value.UpdatedBy).HasMaxLength(100).IsRequired();
        entity.Property(value => value.RecordStatus).HasMaxLength(1).IsRequired();
    }
}
