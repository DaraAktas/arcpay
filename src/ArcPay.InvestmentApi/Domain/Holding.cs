using ArcPay.Shared;

namespace ArcPay.InvestmentApi.Domain;

public sealed class Holding : BaseEntity
{
    private Holding() { }
    public int PortfolioId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal AverageCost { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    internal static Holding Create(string symbol, decimal quantity, decimal unitPrice, string currency, string actor) => new()
    {
        Symbol = symbol,
        Quantity = quantity,
        AverageCost = unitPrice,
        Currency = currency,
        CreatedBy = actor,
        UpdatedBy = actor
    };

    internal void AddPurchase(decimal quantity, decimal unitPrice, string actor)
    {
        var totalCost = (Quantity * AverageCost) + (quantity * unitPrice);
        Quantity += quantity;
        AverageCost = decimal.Round(totalCost / Quantity, 8, MidpointRounding.AwayFromZero);
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = actor;
    }
}
