namespace ArcPay.InvestmentApi.Domain;

public sealed record Quote(
    string Symbol,
    string Name,
    decimal Price,
    string Currency,
    decimal ChangePercent,
    DateTimeOffset AsOf,
    string Source);
