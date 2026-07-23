using ArcPay.WalletApi.Domain;
using ArcPay.WalletApi.Domain.ValueObjects;
using ArcPay.WalletApi.Domain.Wallets;

namespace ArcPay.WalletApi.Tests.Domain;

public sealed class WalletTests
{
    private static readonly CustomerNumber Owner = CustomerNumber.Create("ARC-1000000001").Value;
    private static readonly Currency Lira = Currency.Create("TRY").Value;

    [Fact]
    public void Open_StartsWithZeroBalanceAndImmutableIdentity()
    {
        var wallet = Wallet.Open(Owner, Lira);

        Assert.Equal(Owner, wallet.CustomerNumber);
        Assert.Equal(Lira, wallet.Currency);
        Assert.Equal(0m, wallet.Balance.Amount);
    }

    [Fact]
    public void Credit_IncreasesBalance()
    {
        var wallet = Wallet.Open(Owner, Lira);
        var amount = Money.Create(125.50m, Lira).Value;

        var result = wallet.Credit(amount, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(125.50m, wallet.Balance.Amount);
    }

    [Fact]
    public void Credit_RejectsDifferentCurrencyWithoutChangingBalance()
    {
        var wallet = Wallet.Open(Owner, Lira);
        var dollars = Money.Create(25m, Currency.Create("USD").Value).Value;

        var result = wallet.Credit(dollars, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.CurrencyMismatch, result.Error);
        Assert.Equal(0m, wallet.Balance.Amount);
    }

    [Fact]
    public void Credit_RejectsEmptyTransactionReference()
    {
        var wallet = Wallet.Open(Owner, Lira);
        var amount = Money.Create(25m, Lira).Value;

        var result = wallet.Credit(amount, Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InvalidTransactionReference, result.Error);
        Assert.Equal(0m, wallet.Balance.Amount);
    }

    [Fact]
    public void Debit_RejectsInsufficientBalanceWithoutMutation()
    {
        var wallet = Wallet.Open(Owner, Lira);
        var amount = Money.Create(1m, Lira).Value;

        var result = wallet.Debit(amount, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InsufficientFunds, result.Error);
        Assert.Equal(0m, wallet.Balance.Amount);
    }

    [Fact]
    public void Debit_DecreasesAnAvailableBalance()
    {
        var wallet = Wallet.Open(Owner, Lira);
        wallet.Credit(Money.Create(100m, Lira).Value, Guid.NewGuid());

        var result = wallet.Debit(Money.Create(35.25m, Lira).Value, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(64.75m, wallet.Balance.Amount);
    }
}
