using ArcPay.Shared;

namespace ArcPay.InvestmentApi.Domain;

public sealed class PurchaseOrder : BaseEntity
{
    private PurchaseOrder() { }
    public Guid PurchaseRef { get; private set; }
    public Guid RefundRef { get; private set; }
    public string CustomerNumber { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public string? FailureReason { get; private set; }

    public static PurchaseOrder Start(Guid reference, string customerNumber, Quote quote, decimal quantity) => new()
    {
        PurchaseRef = reference,
        RefundRef = CreateRefundReference(reference),
        CustomerNumber = customerNumber,
        Symbol = quote.Symbol,
        Quantity = quantity,
        UnitPrice = quote.Price,
        TotalAmount = decimal.Round(quote.Price * quantity, 8, MidpointRounding.AwayFromZero),
        Currency = quote.Currency,
        CreatedBy = customerNumber,
        UpdatedBy = customerNumber
    };

    public void Complete() => ChangeStatus("Completed", null);
    public void Compensate(string reason) => ChangeStatus("Compensated", reason);
    public void MarkCompensationFailed(string reason) => ChangeStatus("CompensationFailed", reason);

    private void ChangeStatus(string status, string? reason)
    {
        Status = status;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = CustomerNumber;
    }

    private static Guid CreateRefundReference(Guid purchaseReference)
    {
        var bytes = purchaseReference.ToByteArray();
        bytes[^1] ^= 0xA5;
        return new Guid(bytes);
    }
}
