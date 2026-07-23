using ArcPay.Shared.Results;
using ArcPay.WalletApi.Domain;

namespace ArcPay.WalletApi.Domain.ValueObjects;

public readonly record struct Currency
{
    private static readonly HashSet<string> SupportedCodes =
        new(StringComparer.Ordinal) { "TRY", "USD", "EUR", "XAU" };

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public static IReadOnlyCollection<string> Supported => SupportedCodes;

    public static Result<Currency> Create(string? code)
    {
        var normalized = code?.Trim().ToUpperInvariant();
        return normalized is not null && SupportedCodes.Contains(normalized)
            ? Result<Currency>.Success(new Currency(normalized))
            : Result<Currency>.Failure(WalletErrors.InvalidCurrency);
    }

    public static Currency FromPersistence(string code)
    {
        var result = Create(code);
        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"Unsupported currency persisted: {code}");
    }

    public override string ToString() => Code;
}
