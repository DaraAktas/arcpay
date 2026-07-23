using ArcPay.Shared.Results;
using ArcPay.WalletApi.Application.Abstractions;
using ArcPay.WalletApi.Domain;
using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;
using ArcPay.WalletApi.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace ArcPay.WalletApi.Infrastructure.Persistence;

public sealed class WalletRepository(WalletDbContext dbContext) : IWalletRepository, IWalletUnitOfWork
{
    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken) =>
        await dbContext.Wallets.AddAsync(wallet, cancellationToken);

    public async Task<IReadOnlyList<Wallet>> ListAsync(
        CustomerNumber owner,
        CancellationToken cancellationToken) =>
        await dbContext.Wallets
            .AsNoTracking()
            .Where(wallet => wallet.CustomerNumber == owner)
            .OrderBy(wallet => wallet.Currency)
            .ToListAsync(cancellationToken);

    public Task<Wallet?> GetAsync(
        CustomerNumber owner,
        Currency currency,
        CancellationToken cancellationToken) =>
        dbContext.Wallets
            .AsNoTracking()
            .SingleOrDefaultAsync(
                wallet => wallet.CustomerNumber == owner && wallet.Currency == currency,
                cancellationToken);

    public Task<Wallet?> GetForUpdateAsync(
        CustomerNumber owner,
        Currency currency,
        CancellationToken cancellationToken) =>
        dbContext.Wallets
            .FromSqlInterpolated($"""
                SELECT * FROM "Wallets"
                WHERE "CustomerNumber" = {owner.Value} AND "Currency" = {currency.Code}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<(Wallet First, Wallet Second)?> GetPairForUpdateAsync(
        int walletA,
        int walletB,
        CancellationToken cancellationToken)
    {
        var firstId = Math.Min(walletA, walletB);
        var secondId = Math.Max(walletA, walletB);
        var wallets = await dbContext.Wallets
            .FromSqlInterpolated($"""
                SELECT * FROM "Wallets"
                WHERE "Id" IN ({firstId}, {secondId})
                ORDER BY "Id"
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

        return wallets.Count == 2 ? (wallets[0], wallets[1]) : null;
    }

    public Task<Transaction?> GetTransactionAsync(
        Guid transactionReference,
        CancellationToken cancellationToken) =>
        dbContext.Transactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                transaction => transaction.TransactionRef == transactionReference,
                cancellationToken);

    public void AddTransaction(Transaction transaction) => dbContext.Transactions.Add(transaction);

    public async Task<IWalletDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfWalletDbTransaction(transaction);
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Wallets_CustomerNumber_Currency"
            })
        {
            return Result.Failure(WalletErrors.AlreadyExists);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Transactions_TransactionRef"
            })
        {
            return Result.Failure(WalletErrors.TransactionReferenceConflict);
        }
    }

    private sealed class EfWalletDbTransaction(IDbContextTransaction transaction) : IWalletDbTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken) => transaction.RollbackAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
