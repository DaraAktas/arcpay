using ArcPay.Shared.Results;

namespace ArcPay.WalletApi.Application.Abstractions;

public interface IWalletUnitOfWork
{
    Task<IWalletDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
