using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;

namespace ArcPay.WalletApi.Tests.Domain;

public sealed class TransactionTests
{
    private static readonly CustomerNumber Sender = CustomerNumber.Create("ARC-1000000001").Value;

    [Fact]
    public void RecordDeposit_CreatesCompletedLedgerEntry()
    {
        var reference = Guid.NewGuid();
        var amount = Money.Create(250.75m, Currency.Create("TRY").Value).Value;

        var transaction = Transaction.RecordDeposit(42, amount, reference);

        Assert.Equal(reference, transaction.TransactionRef);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(TransactionStatus.Completed, transaction.Status);
        Assert.Null(transaction.SenderWalletId);
        Assert.Equal(42, transaction.ReceiverWalletId);
        Assert.Equal(amount, transaction.Amount);
    }

    [Fact]
    public void RecordDeposit_RejectsEmptyReference()
    {
        var amount = Money.Create(10m, Currency.Create("TRY").Value).Value;

        Assert.Throws<ArgumentException>(() => Transaction.RecordDeposit(42, amount, Guid.Empty));
    }

    [Fact]
    public void RecordTransfer_CreatesCompletedLedgerEntry()
    {
        var reference = Guid.NewGuid();
        var amount = Money.Create(125.50m, Currency.Create("TRY").Value).Value;

        var transaction = Transaction.RecordTransfer(10, 20, amount, reference, Sender, "  Kira payı  ");

        Assert.Equal(reference, transaction.TransactionRef);
        Assert.Equal(TransactionType.Transfer, transaction.Type);
        Assert.Equal(TransactionStatus.Completed, transaction.Status);
        Assert.Equal(10, transaction.SenderWalletId);
        Assert.Equal(20, transaction.ReceiverWalletId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal("Kira payı", transaction.Description);
        Assert.Equal(Sender.Value, transaction.CreatedBy);
    }

    [Fact]
    public void RecordTransfer_RejectsSameWallet()
    {
        var amount = Money.Create(10m, Currency.Create("TRY").Value).Value;

        Assert.Throws<ArgumentException>(() =>
            Transaction.RecordTransfer(42, 42, amount, Guid.NewGuid(), Sender, null));
    }
}
