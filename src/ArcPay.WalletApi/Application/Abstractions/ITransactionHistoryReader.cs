using ArcPay.WalletApi.Application.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;

namespace ArcPay.WalletApi.Application.Abstractions;

public interface ITransactionHistoryReader
{
    Task<IReadOnlyList<TransactionHistoryView>> ListHistoryAsync(
        CustomerNumber owner,
        CancellationToken cancellationToken);
}
