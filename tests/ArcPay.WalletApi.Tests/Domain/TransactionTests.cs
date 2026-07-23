using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;

namespace ArcPay.WalletApi.Tests.Domain;

public sealed class TransactionTests
{
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
}
