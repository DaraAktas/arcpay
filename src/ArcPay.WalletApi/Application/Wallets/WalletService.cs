using ArcPay.Shared.Results;
using ArcPay.WalletApi.Application.Abstractions;
using ArcPay.WalletApi.Domain;
using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;
using ArcPay.WalletApi.Domain.Wallets;

namespace ArcPay.WalletApi.Application.Wallets;

public sealed class WalletService(IWalletRepository repository, IWalletUnitOfWork unitOfWork)
{
    public async Task<Result<WalletView>> OpenAsync(
        CustomerNumber owner,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var currencyResult = Currency.Create(currencyCode);
        if (currencyResult.IsFailure)
        {
            return Result<WalletView>.Failure(currencyResult.Error);
        }

        var currency = currencyResult.Value;
        if (await repository.GetAsync(owner, currency, cancellationToken) is not null)
        {
            return Result<WalletView>.Failure(WalletErrors.AlreadyExists);
        }

        var wallet = Wallet.Open(owner, currency);
        await repository.AddAsync(wallet, cancellationToken);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsSuccess
            ? Result<WalletView>.Success(WalletView.From(wallet))
            : Result<WalletView>.Failure(saveResult.Error);
    }

    public async Task<IReadOnlyList<WalletView>> ListAsync(
        CustomerNumber owner,
        CancellationToken cancellationToken)
    {
        var wallets = await repository.ListAsync(owner, cancellationToken);
        return wallets.Select(WalletView.From).ToArray();
    }

    public async Task<Result<DepositView>> DepositAsync(
        CustomerNumber owner,
        string currencyCode,
        decimal amount,
        Guid transactionReference,
        CancellationToken cancellationToken)
    {
        var currencyResult = Currency.Create(currencyCode);
        if (currencyResult.IsFailure)
        {
            return Result<DepositView>.Failure(currencyResult.Error);
        }

        var moneyResult = Money.Create(amount, currencyResult.Value);
        if (moneyResult.IsFailure)
        {
            return Result<DepositView>.Failure(moneyResult.Error);
        }

        if (transactionReference == Guid.Empty)
        {
            return Result<DepositView>.Failure(WalletErrors.InvalidTransactionReference);
        }

        await using var dbTransaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var wallet = await repository.GetForUpdateAsync(owner, currencyResult.Value, cancellationToken);
        if (wallet is null)
        {
            return Result<DepositView>.Failure(WalletErrors.NotFound);
        }

        var existingTransaction = await repository.GetTransactionAsync(transactionReference, cancellationToken);
        if (existingTransaction is not null)
        {
            return IsSameDeposit(existingTransaction, wallet, moneyResult.Value)
                ? Result<DepositView>.Success(new DepositView(transactionReference, WalletView.From(wallet)))
                : Result<DepositView>.Failure(WalletErrors.TransactionReferenceConflict);
        }

        var creditResult = wallet.Credit(moneyResult.Value, transactionReference);
        if (creditResult.IsFailure)
        {
            return Result<DepositView>.Failure(creditResult.Error);
        }

        repository.AddTransaction(Transaction.RecordDeposit(wallet.Id, moneyResult.Value, transactionReference));
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            return Result<DepositView>.Failure(saveResult.Error);
        }

        await dbTransaction.CommitAsync(cancellationToken);
        return Result<DepositView>.Success(
            new DepositView(transactionReference, WalletView.From(wallet)));
    }

    private static bool IsSameDeposit(Transaction transaction, Wallet wallet, Money amount) =>
        transaction.Type == TransactionType.Deposit &&
        transaction.Status == TransactionStatus.Completed &&
        transaction.ReceiverWalletId == wallet.Id &&
        transaction.Amount == amount;
}
