using ArcPay.InvestmentApi.Application.Abstractions;
using ArcPay.InvestmentApi.Domain;
using ArcPay.Shared.Results;
using Microsoft.Extensions.Caching.Memory;

namespace ArcPay.InvestmentApi.Application;

public sealed class MarketService(IMarketDataProvider provider, IMemoryCache cache, IConfiguration configuration)
{
    private readonly string[] _symbols = configuration.GetSection("MarketData:Symbols").Get<string[]>() ?? ["AAPL", "MSFT", "TSLA"];

    public async Task<IReadOnlyList<Quote>> ListAsync(CancellationToken cancellationToken)
    {
        var quotes = new List<Quote>();
        foreach (var symbol in _symbols)
        {
            var result = await GetQuoteAsync(symbol, cancellationToken);
            if (result.IsSuccess) quotes.Add(result.Value);
        }
        return quotes;
    }

    public Task<Result<Quote>> GetQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = symbol.Trim().ToUpperInvariant();
        if (!_symbols.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            return Task.FromResult(Result<Quote>.Failure(InvestmentErrors.InvalidSymbol));

        return cache.GetOrCreateAsync($"market-quote:{normalized}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            return await provider.GetQuoteAsync(normalized, cancellationToken);
        })!;
    }
}
