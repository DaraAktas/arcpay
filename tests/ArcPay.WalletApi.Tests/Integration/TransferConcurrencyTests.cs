using ArcPay.Shared.Results;
using ArcPay.WalletApi.Application.Transactions;
using ArcPay.WalletApi.Domain;
using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;
using ArcPay.WalletApi.Domain.Wallets;
using ArcPay.WalletApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArcPay.WalletApi.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public sealed class TransferConcurrencyTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private static readonly CustomerNumber Alice = CustomerNumber.Create("ARC-1000000001").Value;
    private static readonly CustomerNumber Bob = CustomerNumber.Create("ARC-1000000002").Value;
    private static readonly Currency Lira = Currency.Create("TRY").Value;

    public async Task InitializeAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TwentyConcurrentTransfers_PreventDoubleSpendingAndPreserveTotalMoney()
    {
        await SeedWalletsAsync(aliceBalance: 100m, bobBalance: 0m);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = Enumerable.Range(0, 20)
            .Select(async _ =>
            {
                await gate.Task;
                return await TransferAsync(Alice, Bob, 10m, Guid.NewGuid());
            })
            .ToArray();

        gate.SetResult();
        var results = await Task.WhenAll(requests).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(10, results.Count(result => result.IsSuccess));
        Assert.Equal(10, results.Count(result =>
            result.IsFailure && result.Error == WalletErrors.InsufficientFunds));
        var balances = await GetBalancesAsync();
        Assert.Equal(0m, balances[Alice]);
        Assert.Equal(100m, balances[Bob]);
        Assert.Equal(100m, balances.Values.Sum());
        Assert.Equal(10, await CountTransfersAsync());
    }

    [Fact]
    public async Task OppositeDirectionTransfers_UseOneLockOrderAndDoNotDeadlock()
    {
        await SeedWalletsAsync(aliceBalance: 1000m, bobBalance: 1000m);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = Enumerable.Range(0, 20)
            .Select(async index =>
            {
                await gate.Task;
                return index % 2 == 0
                    ? await TransferAsync(Alice, Bob, 1m, Guid.NewGuid())
                    : await TransferAsync(Bob, Alice, 1m, Guid.NewGuid());
            })
            .ToArray();

        gate.SetResult();
        var results = await Task.WhenAll(requests).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        var balances = await GetBalancesAsync();
        Assert.Equal(1000m, balances[Alice]);
        Assert.Equal(1000m, balances[Bob]);
        Assert.Equal(2000m, balances.Values.Sum());
        Assert.Equal(20, await CountTransfersAsync());
    }

    [Fact]
    public async Task ReplayedTransactionReference_ReturnsOriginalWithoutMovingMoneyTwice()
    {
        await SeedWalletsAsync(aliceBalance: 100m, bobBalance: 0m);
        var transactionReference = Guid.NewGuid();

        var first = await TransferAsync(Alice, Bob, 25m, transactionReference);
        var replay = await TransferAsync(Alice, Bob, 25m, transactionReference);

        Assert.True(first.IsSuccess);
        Assert.False(first.Value.IsReplay);
        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value.IsReplay);
        var balances = await GetBalancesAsync();
        Assert.Equal(75m, balances[Alice]);
        Assert.Equal(25m, balances[Bob]);
        Assert.Equal(1, await CountTransfersAsync());
    }

    [Fact]
    public async Task ConcurrentIdenticalReferences_MoveMoneyExactlyOnce()
    {
        await SeedWalletsAsync(aliceBalance: 100m, bobBalance: 0m);
        var transactionReference = Guid.NewGuid();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = Enumerable.Range(0, 20)
            .Select(async _ =>
            {
                await gate.Task;
                return await TransferAsync(Alice, Bob, 10m, transactionReference);
            })
            .ToArray();

        gate.SetResult();
        var results = await Task.WhenAll(requests).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Single(results, result => !result.Value.IsReplay);
        Assert.Equal(19, results.Count(result => result.Value.IsReplay));
        var balances = await GetBalancesAsync();
        Assert.Equal(90m, balances[Alice]);
        Assert.Equal(10m, balances[Bob]);
        Assert.Equal(1, await CountTransfersAsync());
    }

    private WalletDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseNpgsql(fixture.Container.GetConnectionString())
            .Options;
        return new WalletDbContext(options);
    }

    private async Task<Result<TransferView>> TransferAsync(
        CustomerNumber sender,
        CustomerNumber receiver,
        decimal amount,
        Guid transactionReference)
    {
        await using var dbContext = CreateDbContext();
        var repository = new WalletRepository(dbContext);
        var service = new TransferService(repository, repository, repository);
        return await service.TransferAsync(
            sender,
            receiver.Value,
            Lira.Code,
            amount,
            transactionReference,
            "Concurrency proof",
            CancellationToken.None);
    }

    private async Task SeedWalletsAsync(decimal aliceBalance, decimal bobBalance)
    {
        await using var dbContext = CreateDbContext();
        var aliceWallet = Wallet.Open(Alice, Lira);
        var bobWallet = Wallet.Open(Bob, Lira);
        dbContext.Wallets.AddRange(aliceWallet, bobWallet);
        await dbContext.SaveChangesAsync();
        await CreditAsync(dbContext, aliceWallet, aliceBalance);
        await CreditAsync(dbContext, bobWallet, bobBalance);
        await dbContext.SaveChangesAsync();
    }

    private static Task CreditAsync(WalletDbContext dbContext, Wallet wallet, decimal balance)
    {
        if (balance == 0)
        {
            return Task.CompletedTask;
        }

        var reference = Guid.NewGuid();
        var amount = Money.Create(balance, wallet.Currency).Value;
        Assert.True(wallet.Credit(amount, reference).IsSuccess);
        dbContext.Transactions.Add(Transaction.RecordDeposit(wallet.Id, amount, reference));
        return Task.CompletedTask;
    }

    private async Task<Dictionary<CustomerNumber, decimal>> GetBalancesAsync()
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Wallets
            .AsNoTracking()
            .ToDictionaryAsync(wallet => wallet.CustomerNumber, wallet => wallet.Balance.Amount);
    }

    private async Task<int> CountTransfersAsync()
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Transactions.CountAsync(transaction => transaction.Type == TransactionType.Transfer);
    }
}
