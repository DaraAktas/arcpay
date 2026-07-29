using System.Globalization;
using System.Text.Json;
using ArcPay.InvestmentApi.Application.Abstractions;
using ArcPay.InvestmentApi.Domain;
using ArcPay.Shared.Results;

namespace ArcPay.InvestmentApi.Infrastructure;

public sealed class AlphaVantageMarketDataProvider(HttpClient httpClient, IConfiguration configuration) : IMarketDataProvider
{
    public async Task<Result<Quote>> GetQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        var apiKey = configuration["MarketData:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return Result<Quote>.Failure(InvestmentErrors.QuoteUnavailable);

        using var response = await httpClient.GetAsync(
            $"query?function=GLOBAL_QUOTE&symbol={Uri.EscapeDataString(symbol)}&apikey={Uri.EscapeDataString(apiKey)}",
            cancellationToken);
        if (!response.IsSuccessStatusCode) return Result<Quote>.Failure(InvestmentErrors.QuoteUnavailable);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("Global Quote", out var globalQuote) ||
            !globalQuote.TryGetProperty("05. price", out var priceNode) ||
            !decimal.TryParse(priceNode.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
            return Result<Quote>.Failure(InvestmentErrors.QuoteUnavailable);

        var change = 0m;
        if (globalQuote.TryGetProperty("10. change percent", out var changeNode))
            decimal.TryParse(changeNode.GetString()?.TrimEnd('%'), NumberStyles.Number, CultureInfo.InvariantCulture, out change);

        return Result<Quote>.Success(new Quote(
            symbol.ToUpperInvariant(), symbol.ToUpperInvariant(), price, "USD", change, DateTimeOffset.UtcNow, "Alpha Vantage"));
    }
}
