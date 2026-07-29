using ArcPay.Shared;
using ArcPay.Shared.Results;

namespace ArcPay.InvestmentApi.Domain;

public sealed class Portfolio : BaseEntity
{
    private readonly List<Holding> _holdings = [];
    private Portfolio() { }
    private Portfolio(string customerNumber)
    {
        CustomerNumber = customerNumber;
        CreatedBy = customerNumber;
        UpdatedBy = customerNumber;
    }

    public string CustomerNumber { get; private set; } = string.Empty;
    public IReadOnlyCollection<Holding> Holdings => _holdings;

    public static Portfolio Open(string customerNumber) => new(customerNumber);

    public Result AddPurchase(string symbol, decimal quantity, decimal unitPrice, string currency)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return Result.Failure(InvestmentErrors.InvalidSymbol);
        if (quantity <= 0 || decimal.Round(quantity, 8) != quantity)
            return Result.Failure(InvestmentErrors.InvalidQuantity);

        var normalized = symbol.Trim().ToUpperInvariant();
        var holding = _holdings.SingleOrDefault(item => item.Symbol == normalized);
        if (holding is null)
        {
            _holdings.Add(Holding.Create(normalized, quantity, unitPrice, currency, CustomerNumber));
        }
        else
        {
            holding.AddPurchase(quantity, unitPrice, CustomerNumber);
        }

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = CustomerNumber;
        return Result.Success();
    }
}
