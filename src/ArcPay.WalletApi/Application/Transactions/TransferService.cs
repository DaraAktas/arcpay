using ArcPay.Shared.Results;
using ArcPay.WalletApi.Application.Abstractions;
using ArcPay.WalletApi.Application.Wallets;
using ArcPay.WalletApi.Domain;
using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;
using ArcPay.WalletApi.Domain.Wallets;

namespace ArcPay.WalletApi.Application.Transactions;

public sealed class TransferService(
    IWalletRepository repository,
    IWalletUnitOfWork unitOfWork,
    ITransactionHistoryReader historyReader)
{
    public Task<IReadOnlyList<TransactionHistoryView>> ListAsync(
        CustomerNumber owner,
        CancellationToken cancellationToken) =>
        historyReader.ListHistoryAsync(owner, cancellationToken);

    public async Task<Result<TransferView>> TransferAsync(
        CustomerNumber senderCustomerNumber,
        string receiverCustomerNumber,
        string currencyCode,
        decimal amount,
        Guid transactionReference,
        string? description,
        CancellationToken cancellationToken)
    {
        var receiverResult = CustomerNumber.Create(receiverCustomerNumber);
        if (receiverResult.IsFailure)
        {
            return Result<TransferView>.Failure(receiverResult.Error);
        }

        if (receiverResult.Value == senderCustomerNumber)
        {
            return Result<TransferView>.Failure(WalletErrors.SelfTransfer);
        }

        var currencyResult = Currency.Create(currencyCode);
        if (currencyResult.IsFailure)
        {
            return Result<TransferView>.Failure(currencyResult.Error);
        }

        var moneyResult = Money.Create(amount, currencyResult.Value);
        if (moneyResult.IsFailure)
        {
            return Result<TransferView>.Failure(moneyResult.Error);
        }

        if (transactionReference == Guid.Empty)
        {
            return Result<TransferView>.Failure(WalletErrors.InvalidTransactionReference);
        }

        await using var dbTransaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var senderLookup = await repository.GetAsync(senderCustomerNumber, currencyResult.Value, cancellationToken);
        var receiverLookup = await repository.GetAsync(receiverResult.Value, currencyResult.Value, cancellationToken);
        if (senderLookup is null || receiverLookup is null)
        {
            return Result<TransferView>.Failure(WalletErrors.NotFound);
        }

        var pair = await repository.GetPairForUpdateAsync(senderLookup.Id, receiverLookup.Id, cancellationToken);
        if (pair is null)
        {
            return Result<TransferView>.Failure(WalletErrors.NotFound);
        }

        var sender = pair.Value.First.Id == senderLookup.Id ? pair.Value.First : pair.Value.Second;
        var receiver = pair.Value.First.Id == receiverLookup.Id ? pair.Value.First : pair.Value.Second;
        var existingTransaction = await repository.GetTransactionAsync(transactionReference, cancellationToken);
        if (existingTransaction is not null)
        {
            return IsSameTransfer(existingTransaction, sender, receiver, moneyResult.Value, description)
                ? Result<TransferView>.Success(ToView(existingTransaction, sender, receiver, true))
                : Result<TransferView>.Failure(WalletErrors.TransactionReferenceConflict);
        }

        var debitResult = sender.Debit(moneyResult.Value, transactionReference);
        if (debitResult.IsFailure)
        {
            return Result<TransferView>.Failure(debitResult.Error);
        }

        var creditResult = receiver.Credit(moneyResult.Value, transactionReference);
        if (creditResult.IsFailure)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            return Result<TransferView>.Failure(creditResult.Error);
        }

        var transaction = Transaction.RecordTransfer(
            sender.Id,
            receiver.Id,
            moneyResult.Value,
            transactionReference,
            senderCustomerNumber,
            description);
        repository.AddTransaction(transaction);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            return Result<TransferView>.Failure(saveResult.Error);
        }

        await dbTransaction.CommitAsync(cancellationToken);
        return Result<TransferView>.Success(ToView(transaction, sender, receiver, false));
    }

    private static bool IsSameTransfer(
        Transaction transaction,
        Wallet sender,
        Wallet receiver,
        Money amount,
        string? description) =>
        transaction.Type == TransactionType.Transfer &&
        transaction.Status == TransactionStatus.Completed &&
        transaction.SenderWalletId == sender.Id &&
        transaction.ReceiverWalletId == receiver.Id &&
        transaction.Amount == amount &&
        transaction.Description == NormalizeDescription(description);

    private static TransferView ToView(
        Transaction transaction,
        Wallet sender,
        Wallet receiver,
        bool isReplay) => new(
        transaction.TransactionRef,
        receiver.CustomerNumber.Value,
        transaction.Amount.Amount,
        transaction.Currency.Code,
        WalletView.From(sender),
        transaction.CreatedAt,
        isReplay);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
