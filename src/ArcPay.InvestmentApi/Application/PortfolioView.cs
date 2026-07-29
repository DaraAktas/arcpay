namespace ArcPay.InvestmentApi.Application;

public sealed record PortfolioView(string CustomerNumber, IReadOnlyList<HoldingView> Holdings);
public sealed record HoldingView(string Symbol, decimal Quantity, decimal AverageCost, string Currency);
public sealed record PurchaseView(
    Guid PurchaseRef,
    string Symbol,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    string Currency,
    string Status,
    bool IsReplay);
