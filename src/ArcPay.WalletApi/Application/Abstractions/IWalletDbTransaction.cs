namespace ArcPay.WalletApi.Application.Abstractions;

public interface IWalletDbTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
