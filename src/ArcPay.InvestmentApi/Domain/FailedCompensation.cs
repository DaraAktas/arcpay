using ArcPay.Shared;

namespace ArcPay.InvestmentApi.Domain;

public sealed class FailedCompensation : BaseEntity
{
    private FailedCompensation() { }
    public Guid PaymentTransactionRef { get; private set; }
    public Guid RefundTransactionRef { get; private set; }
    public string CustomerNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public bool Resolved { get; private set; }

    public static FailedCompensation Record(PurchaseOrder order, string reason) => new()
    {
        PaymentTransactionRef = order.PurchaseRef,
        RefundTransactionRef = order.RefundRef,
        CustomerNumber = order.CustomerNumber,
        Amount = order.TotalAmount,
        Currency = order.Currency,
        Reason = reason,
        CreatedBy = "investment-saga",
        UpdatedBy = "investment-saga"
    };
}
