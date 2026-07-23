using ArcPay.Shared.Results;
using ArcPay.WalletApi.Domain;

namespace ArcPay.WalletApi.Domain.ValueObjects;

public readonly record struct Money
{
    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public Currency Currency { get; }

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        return amount > 0 && GetScale(amount) <= 8
            ? Result<Money>.Success(new Money(amount, currency))
            : Result<Money>.Failure(WalletErrors.InvalidAmount);
    }

    public static Money FromBalance(decimal amount, Currency currency)
    {
        return amount >= 0 && GetScale(amount) <= 8
            ? new Money(amount, currency)
            : throw new InvalidOperationException("A persisted wallet balance cannot be negative or exceed 8 decimal places.");
    }

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return FromBalance(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return FromBalance(left.Amount - right.Amount, left.Currency);
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(WalletErrors.CurrencyMismatch.Description);
        }
    }

    private static int GetScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xFF;
}
