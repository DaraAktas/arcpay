using ArcPay.InvestmentApi.Application;
using ArcPay.InvestmentApi.Application.Abstractions;
using ArcPay.InvestmentApi.Domain;
using ArcPay.InvestmentApi.Infrastructure;
using ArcPay.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace ArcPay.InvestmentApi.Tests;

public sealed class PurchaseSagaTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("arcpay_investment_tests")
        .WithUsername("postgres")
        .WithPassword("ArcPay-Test-Password-42!")
        .Build();

    static PurchaseSagaTests() => Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Purchase_WhenPortfolioWriteFails_RefundsWalletAndRecordsCompensatedOrder()
    {
        await using var dbContext = await CreateDbContextAsync();
        var wallet = new FakeWalletGateway();
        var service = CreateService(dbContext, wallet);
        var reference = Guid.NewGuid();

        var result = await service.PurchaseAsync(
            "ARC-1000000001", "AAPL", 1m, reference, true, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(InvestmentErrors.PurchaseCompensated, result.Error);
        Assert.Equal(1, wallet.ChargeCount);
        Assert.Equal(1, wallet.RefundCount);
        var order = await dbContext.PurchaseOrders.SingleAsync(item => item.PurchaseRef == reference);
        Assert.Equal("Compensated", order.Status);
        Assert.Empty(await dbContext.Portfolios.ToListAsync());
    }

    [Fact]
    public async Task Purchase_ReplayedReference_DoesNotChargeTwice()
    {
        await using var dbContext = await CreateDbContextAsync();
        var wallet = new FakeWalletGateway();
        var service = CreateService(dbContext, wallet);
        var reference = Guid.NewGuid();

        var first = await service.PurchaseAsync(
            "ARC-1000000001", "AAPL", 2m, reference, false, CancellationToken.None);
        var replay = await service.PurchaseAsync(
            "ARC-1000000001", "AAPL", 2m, reference, false, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value.IsReplay);
        Assert.Equal(1, wallet.ChargeCount);
        Assert.Equal(2m, (await dbContext.Portfolios.Include(item => item.Holdings).SingleAsync()).Holdings.Single().Quantity);
    }

    private async Task<InvestmentDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<InvestmentDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
        var context = new InvestmentDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }

    private static PurchaseService CreateService(InvestmentDbContext dbContext, FakeWalletGateway wallet)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Symbols:0"] = "AAPL"
        }).Build();
        var market = new MarketService(new FakeMarketProvider(), new MemoryCache(new MemoryCacheOptions()), config);
        return new PurchaseService(dbContext, market, wallet, new DevelopmentEnvironment(), NullLogger<PurchaseService>.Instance);
    }

    private sealed class FakeMarketProvider : IMarketDataProvider
    {
        public Task<Result<Quote>> GetQuoteAsync(string symbol, CancellationToken cancellationToken) =>
            Task.FromResult(Result<Quote>.Success(new(symbol, "Apple", 100m, "USD", 1m, DateTimeOffset.UtcNow, "Test")));
    }

    private sealed class FakeWalletGateway : IWalletPaymentGateway
    {
        public int ChargeCount { get; private set; }
        public int RefundCount { get; private set; }
        public Task<Result<WalletPayment>> ChargeAsync(decimal amount, string currency, Guid reference, string description, CancellationToken cancellationToken)
        {
            ChargeCount++;
            return Task.FromResult(Result<WalletPayment>.Success(new(reference, amount, currency, false)));
        }
        public Task<Result<WalletPayment>> RefundAsync(Guid originalReference, Guid refundReference, CancellationToken cancellationToken)
        {
            RefundCount++;
            return Task.FromResult(Result<WalletPayment>.Success(new(refundReference, 100m, "USD", false)));
        }
    }

    private sealed class DevelopmentEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ArcPay.InvestmentApi.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
