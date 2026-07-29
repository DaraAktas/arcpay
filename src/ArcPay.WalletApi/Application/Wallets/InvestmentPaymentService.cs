using ArcPay.Shared.Results;
using ArcPay.WalletApi.Application.Abstractions;
using ArcPay.WalletApi.Domain;
using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;
using ArcPay.WalletApi.Domain.Wallets;

namespace ArcPay.WalletApi.Application.Wallets;

public sealed class InvestmentPaymentService(IWalletRepository repository, IWalletUnitOfWork unitOfWork)
{
    public async Task<Result<InvestmentPaymentView>> ChargeAsync(
        CustomerNumber owner,
        decimal amount,
        string currencyCode,
        Guid transactionReference,
        string? description,
        CancellationToken cancellationToken)
    {
        var currencyResult = Currency.Create(currencyCode);
        if (currencyResult.IsFailure) return Result<InvestmentPaymentView>.Failure(currencyResult.Error);
        var moneyResult = Money.Create(amount, currencyResult.Value);
        if (moneyResult.IsFailure) return Result<InvestmentPaymentView>.Failure(moneyResult.Error);
        if (transactionReference == Guid.Empty)
            return Result<InvestmentPaymentView>.Failure(WalletErrors.InvalidTransactionReference);

        await using var dbTransaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var wallet = await repository.GetForUpdateAsync(owner, currencyResult.Value, cancellationToken);
        if (wallet is null) return Result<InvestmentPaymentView>.Failure(WalletErrors.NotFound);

        var existing = await repository.GetTransactionAsync(transactionReference, cancellationToken);
        if (existing is not null)
        {
            return existing.Type == TransactionType.InvestmentPurchase &&
                   existing.SenderWalletId == wallet.Id && existing.Amount == moneyResult.Value
                ? Result<InvestmentPaymentView>.Success(ToView(existing, true))
                : Result<InvestmentPaymentView>.Failure(WalletErrors.TransactionReferenceConflict);
        }

        var debitResult = wallet.Debit(moneyResult.Value, transactionReference);
        if (debitResult.IsFailure) return Result<InvestmentPaymentView>.Failure(debitResult.Error);

        var transaction = Transaction.RecordInvestmentPurchase(
            wallet.Id, moneyResult.Value, transactionReference, owner, description);
        repository.AddTransaction(transaction);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure) return Result<InvestmentPaymentView>.Failure(saveResult.Error);

        await dbTransaction.CommitAsync(cancellationToken);
        return Result<InvestmentPaymentView>.Success(ToView(transaction, false));
    }

    public async Task<Result<InvestmentPaymentView>> RefundAsync(
        CustomerNumber owner,
        Guid originalReference,
        Guid refundReference,
        CancellationToken cancellationToken)
    {
        if (originalReference == Guid.Empty || refundReference == Guid.Empty || originalReference == refundReference)
            return Result<InvestmentPaymentView>.Failure(WalletErrors.InvalidTransactionReference);

        await using var dbTransaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var original = await repository.GetTransactionAsync(originalReference, cancellationToken);
        if (original is not { Type: TransactionType.InvestmentPurchase, SenderWalletId: int walletId })
            return Result<InvestmentPaymentView>.Failure(WalletErrors.NotFound);

        var wallet = await repository.GetByIdForUpdateAsync(walletId, cancellationToken);
        if (wallet is null || wallet.CustomerNumber != owner)
            return Result<InvestmentPaymentView>.Failure(WalletErrors.NotFound);

        var existingRefund = await repository.GetTransactionAsync(refundReference, cancellationToken);
        if (existingRefund is not null)
        {
            return existingRefund.Type == TransactionType.InvestmentRefund &&
                   existingRefund.ReceiverWalletId == wallet.Id && existingRefund.Amount == original.Amount
                ? Result<InvestmentPaymentView>.Success(ToView(existingRefund, true))
                : Result<InvestmentPaymentView>.Failure(WalletErrors.TransactionReferenceConflict);
        }

        if (wallet.RecordStatus != "A") wallet.Reopen();
        var creditResult = wallet.Credit(original.Amount, refundReference);
        if (creditResult.IsFailure) return Result<InvestmentPaymentView>.Failure(creditResult.Error);

        var refund = Transaction.RecordInvestmentRefund(
            wallet.Id, original.Amount, refundReference, owner, $"Refund for {originalReference}");
        repository.AddTransaction(refund);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure) return Result<InvestmentPaymentView>.Failure(saveResult.Error);

        await dbTransaction.CommitAsync(cancellationToken);
        return Result<InvestmentPaymentView>.Success(ToView(refund, false));
    }

    private static InvestmentPaymentView ToView(Transaction transaction, bool isReplay) =>
        new(transaction.TransactionRef, transaction.Amount.Amount, transaction.Currency.Code, isReplay);
}
