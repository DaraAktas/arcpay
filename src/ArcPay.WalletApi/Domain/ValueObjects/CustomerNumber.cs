using System.Text.RegularExpressions;
using ArcPay.Shared.Results;
using ArcPay.WalletApi.Domain;

namespace ArcPay.WalletApi.Domain.ValueObjects;

public readonly partial record struct CustomerNumber
{
    private CustomerNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<CustomerNumber> Create(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return normalized is not null && CustomerNumberPattern().IsMatch(normalized)
            ? Result<CustomerNumber>.Success(new CustomerNumber(normalized))
            : Result<CustomerNumber>.Failure(WalletErrors.InvalidCustomerNumber);
    }

    public static CustomerNumber FromPersistence(string value)
    {
        var result = Create(value);
        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"Invalid customer number persisted: {value}");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^ARC-[0-9]{10}$", RegexOptions.CultureInvariant)]
    private static partial Regex CustomerNumberPattern();
}
