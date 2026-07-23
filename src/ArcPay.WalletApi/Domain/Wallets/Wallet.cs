using ArcPay.Shared;
using ArcPay.Shared.Results;
using ArcPay.WalletApi.Domain.ValueObjects;

namespace ArcPay.WalletApi.Domain.Wallets;

public sealed class Wallet : BaseEntity
{
    private decimal _balanceAmount;

    private Wallet()
    {
    }

    private Wallet(CustomerNumber customerNumber, Currency currency)
    {
        CustomerNumber = customerNumber;
        Currency = currency;
        _balanceAmount = 0;
        CreatedBy = customerNumber.Value;
        UpdatedBy = customerNumber.Value;
    }

    public CustomerNumber CustomerNumber { get; private set; }
    public Currency Currency { get; private set; }
    public Money Balance => Money.FromBalance(_balanceAmount, Currency);

    public static Wallet Open(CustomerNumber owner, Currency currency) => new(owner, currency);

    public Result Credit(Money amount, Guid transactionReference)
    {
        var validation = ValidateMutation(amount, transactionReference);
        if (validation.IsFailure)
        {
            return validation;
        }

        _balanceAmount += amount.Amount;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = CustomerNumber.Value;
        return Result.Success();
    }

    public Result Debit(Money amount, Guid transactionReference)
    {
        var validation = ValidateMutation(amount, transactionReference);
        if (validation.IsFailure)
        {
            return validation;
        }

        if (_balanceAmount < amount.Amount)
        {
            return Result.Failure(WalletErrors.InsufficientFunds);
        }

        _balanceAmount -= amount.Amount;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = CustomerNumber.Value;
        return Result.Success();
    }

    private Result ValidateMutation(Money amount, Guid transactionReference)
    {
        if (amount.Currency != Currency)
        {
            return Result.Failure(WalletErrors.CurrencyMismatch);
        }

        return transactionReference == Guid.Empty
            ? Result.Failure(WalletErrors.InvalidTransactionReference)
            : Result.Success();
    }
}
