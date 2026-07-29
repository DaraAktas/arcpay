using ArcPay.InvestmentApi.Application.Abstractions;
using ArcPay.InvestmentApi.Domain;
using ArcPay.Shared.Results;

namespace ArcPay.InvestmentApi.Infrastructure;

public sealed class DevelopmentMarketDataProvider : IMarketDataProvider
{
    private static readonly IReadOnlyDictionary<string, (string Name, decimal Price, decimal Change)> Quotes =
        new Dictionary<string, (string, decimal, decimal)>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = ("Apple", 210.42m, 1.18m),
            ["MSFT"] = ("Microsoft", 512.67m, -0.34m),
            ["TSLA"] = ("Tesla", 328.15m, 2.71m)
        };

    public Task<Result<Quote>> GetQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        if (!Quotes.TryGetValue(symbol, out var quote))
            return Task.FromResult(Result<Quote>.Failure(InvestmentErrors.InvalidSymbol));

        return Task.FromResult(Result<Quote>.Success(new Quote(
            symbol.ToUpperInvariant(), quote.Name, quote.Price, "USD", quote.Change, DateTimeOffset.UtcNow, "ArcPay Demo Market")));
    }
}
