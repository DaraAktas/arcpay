using ArcPay.InvestmentApi.Application;
using ArcPay.InvestmentApi.Application.Abstractions;
using ArcPay.InvestmentApi.Domain;
using ArcPay.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ArcPay.InvestmentApi.Tests;

public sealed class MarketServiceTests
{
    [Fact]
    public async Task GetQuote_ReusesCachedValueWithinSixtySeconds()
    {
        var provider = new CountingProvider();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Symbols:0"] = "AAPL"
        }).Build();
        var service = new MarketService(provider, new MemoryCache(new MemoryCacheOptions()), configuration);

        var first = await service.GetQuoteAsync("AAPL", CancellationToken.None);
        var second = await service.GetQuoteAsync("aapl", CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, provider.CallCount);
        Assert.Same(first.Value, second.Value);
    }

    private sealed class CountingProvider : IMarketDataProvider
    {
        public int CallCount { get; private set; }
        public Task<Result<Quote>> GetQuoteAsync(string symbol, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(Result<Quote>.Success(
                new(symbol, "Apple", 100m, "USD", 0m, DateTimeOffset.UtcNow, "Test")));
        }
    }
}
