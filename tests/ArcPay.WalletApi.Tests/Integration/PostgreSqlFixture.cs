using Testcontainers.PostgreSql;

namespace ArcPay.WalletApi.Tests.Integration;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    static PostgreSqlFixture()
    {
        // The fixture owns and explicitly disposes its container; this avoids a second helper
        // container while keeping the integration suite deterministic on Docker Desktop and CI.
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    }

    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("arcpay_phase4_tests")
        .WithUsername("postgres")
        .WithPassword("ArcPay-Test-Password-42!")
        .Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL integration";
}
