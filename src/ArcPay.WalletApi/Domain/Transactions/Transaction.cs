using ArcPay.Shared;
using ArcPay.WalletApi.Domain.ValueObjects;

namespace ArcPay.WalletApi.Domain.Transactions;

public sealed class Transaction : BaseEntity
{
    private decimal _amount;

    private Transaction()
    {
    }

    public Guid TransactionRef { get; private set; }
    public TransactionType Type { get; private set; }
    public int? SenderWalletId { get; private set; }
    public int? ReceiverWalletId { get; private set; }
    public Currency Currency { get; private set; }
    public Money Amount => Money.FromBalance(_amount, Currency);
    public TransactionStatus Status { get; private set; }
    public string? Description { get; private set; }

    public static Transaction RecordDeposit(int receiverWalletId, Money amount, Guid transactionReference)
    {
        if (receiverWalletId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(receiverWalletId));
        }

        if (transactionReference == Guid.Empty)
        {
            throw new ArgumentException(WalletErrors.InvalidTransactionReference.Description, nameof(transactionReference));
        }

        return new Transaction
        {
            TransactionRef = transactionReference,
            Type = TransactionType.Deposit,
            ReceiverWalletId = receiverWalletId,
            _amount = amount.Amount,
            Currency = amount.Currency,
            Status = TransactionStatus.Completed,
            Description = "Wallet deposit",
            CreatedBy = "deposit",
            UpdatedBy = "deposit"
        };
    }

    public static Transaction RecordTransfer(
        int senderWalletId,
        int receiverWalletId,
        Money amount,
        Guid transactionReference,
        CustomerNumber initiatedBy,
        string? description)
    {
        if (senderWalletId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(senderWalletId));
        }

        if (receiverWalletId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(receiverWalletId));
        }

        if (senderWalletId == receiverWalletId)
        {
            throw new ArgumentException(WalletErrors.SelfTransfer.Description, nameof(receiverWalletId));
        }

        if (transactionReference == Guid.Empty)
        {
            throw new ArgumentException(WalletErrors.InvalidTransactionReference.Description, nameof(transactionReference));
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return new Transaction
        {
            TransactionRef = transactionReference,
            Type = TransactionType.Transfer,
            SenderWalletId = senderWalletId,
            ReceiverWalletId = receiverWalletId,
            _amount = amount.Amount,
            Currency = amount.Currency,
            Status = TransactionStatus.Completed,
            Description = normalizedDescription,
            CreatedBy = initiatedBy.Value,
            UpdatedBy = initiatedBy.Value
        };
    }

    public static Transaction RecordInvestmentPurchase(
        int senderWalletId,
        Money amount,
        Guid transactionReference,
        CustomerNumber initiatedBy,
        string? description) => RecordWalletMovement(
            TransactionType.InvestmentPurchase,
            senderWalletId,
            null,
            amount,
            transactionReference,
            initiatedBy,
            description ?? "Investment purchase");

    public static Transaction RecordInvestmentRefund(
        int receiverWalletId,
        Money amount,
        Guid transactionReference,
        CustomerNumber initiatedBy,
        string? description) => RecordWalletMovement(
            TransactionType.InvestmentRefund,
            null,
            receiverWalletId,
            amount,
            transactionReference,
            initiatedBy,
            description ?? "Investment compensation refund");

    private static Transaction RecordWalletMovement(
        TransactionType type,
        int? senderWalletId,
        int? receiverWalletId,
        Money amount,
        Guid transactionReference,
        CustomerNumber initiatedBy,
        string description)
    {
        if (senderWalletId is <= 0 || receiverWalletId is <= 0)
            throw new ArgumentOutOfRangeException(nameof(senderWalletId));
        if (transactionReference == Guid.Empty)
            throw new ArgumentException(WalletErrors.InvalidTransactionReference.Description, nameof(transactionReference));

        return new Transaction
        {
            TransactionRef = transactionReference,
            Type = type,
            SenderWalletId = senderWalletId,
            ReceiverWalletId = receiverWalletId,
            _amount = amount.Amount,
            Currency = amount.Currency,
            Status = TransactionStatus.Completed,
            Description = description.Trim(),
            CreatedBy = initiatedBy.Value,
            UpdatedBy = initiatedBy.Value
        };
    }
}
