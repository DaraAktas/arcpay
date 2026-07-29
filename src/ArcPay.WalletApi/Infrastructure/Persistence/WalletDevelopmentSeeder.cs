using ArcPay.WalletApi.Domain.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;
using ArcPay.WalletApi.Domain.Wallets;
using Microsoft.EntityFrameworkCore;

namespace ArcPay.WalletApi.Infrastructure.Persistence;

public sealed class WalletDevelopmentSeeder(WalletDbContext dbContext)
{
    private static readonly (string CustomerNumber, string Currency, decimal Balance, Guid Reference)[] DemoWallets =
    [
        ("ARC-9000000001", "TRY", 1000m, Guid.Parse("90000001-0000-0000-0000-000000000001")),
        ("ARC-9000000001", "USD", 2000m, Guid.Parse("90000001-0000-0000-0000-000000000011")),
        ("ARC-9000000002", "TRY", 250m, Guid.Parse("90000002-0000-0000-0000-000000000002")),
        ("ARC-9000000003", "TRY", 0m, Guid.Parse("90000003-0000-0000-0000-000000000003"))
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var demo in DemoWallets)
        {
            var owner = CustomerNumber.Create(demo.CustomerNumber).Value;
            var currency = Currency.Create(demo.Currency).Value;
            if (await dbContext.Wallets.AnyAsync(
                    wallet => wallet.CustomerNumber == owner && wallet.Currency == currency,
                    cancellationToken)) continue;

            var wallet = Wallet.Open(owner, currency);
            dbContext.Wallets.Add(wallet);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (demo.Balance <= 0) continue;
            var amount = Money.Create(demo.Balance, currency).Value;
            wallet.Credit(amount, demo.Reference);
            dbContext.Transactions.Add(Transaction.RecordDeposit(wallet.Id, amount, demo.Reference));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
