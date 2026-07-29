using ArcPay.InvestmentApi.Domain;

namespace ArcPay.InvestmentApi.Tests;

public sealed class PortfolioTests
{
    [Fact]
    public void AddPurchase_CalculatesWeightedAverageAndKeepsOneHolding()
    {
        var portfolio = Portfolio.Open("ARC-1000000001");

        portfolio.AddPurchase("AAPL", 2m, 100m, "USD");
        var result = portfolio.AddPurchase("aapl", 1m, 160m, "USD");

        Assert.True(result.IsSuccess);
        var holding = Assert.Single(portfolio.Holdings);
        Assert.Equal(3m, holding.Quantity);
        Assert.Equal(120m, holding.AverageCost);
    }

    [Fact]
    public void AddPurchase_RejectsInvalidQuantityWithoutMutation()
    {
        var portfolio = Portfolio.Open("ARC-1000000001");

        var result = portfolio.AddPurchase("AAPL", 0m, 100m, "USD");

        Assert.True(result.IsFailure);
        Assert.Empty(portfolio.Holdings);
    }
}
