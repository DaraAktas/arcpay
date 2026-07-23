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
}
