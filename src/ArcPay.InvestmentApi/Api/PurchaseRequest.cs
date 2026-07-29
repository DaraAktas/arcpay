namespace ArcPay.InvestmentApi.Api;

public sealed record PurchaseRequest(string Symbol, decimal Quantity, Guid PurchaseRef, bool SimulatePortfolioFailure = false);
