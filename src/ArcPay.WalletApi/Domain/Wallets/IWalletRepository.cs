using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;

namespace ArcPay.WalletApi.Domain.Wallets;

public interface IWalletRepository
{
    Task AddAsync(Wallet wallet, CancellationToken cancellationToken);
    Task<IReadOnlyList<Wallet>> ListAsync(CustomerNumber owner, CancellationToken cancellationToken);
    Task<Wallet?> GetAsync(CustomerNumber owner, Currency currency, CancellationToken cancellationToken);
    Task<Wallet?> GetAnyAsync(CustomerNumber owner, Currency currency, CancellationToken cancellationToken);
    Task<Wallet?> GetForUpdateAsync(CustomerNumber owner, Currency currency, CancellationToken cancellationToken);
    Task<Wallet?> GetByIdForUpdateAsync(int walletId, CancellationToken cancellationToken);
    Task<(Wallet First, Wallet Second)?> GetPairForUpdateAsync(
        int walletA,
        int walletB,
        CancellationToken cancellationToken);
    Task<Transaction?> GetTransactionAsync(Guid transactionReference, CancellationToken cancellationToken);
    void AddTransaction(Transaction transaction);
}
