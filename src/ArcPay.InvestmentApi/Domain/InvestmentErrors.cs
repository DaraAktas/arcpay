using ArcPay.Shared.Results;

namespace ArcPay.InvestmentApi.Domain;

public static class InvestmentErrors
{
    public static readonly Error InvalidSymbol = new("investment.invalid_symbol", "Investment symbol is invalid.", 400);
    public static readonly Error InvalidQuantity = new("investment.invalid_quantity", "Quantity must be positive and have at most 8 decimal places.", 400);
    public static readonly Error QuoteUnavailable = new("investment.quote_unavailable", "Market quote is temporarily unavailable.", 503);
    public static readonly Error PurchaseReferenceConflict = new("investment.purchase_reference_conflict", "Purchase reference was already used for another order.", 409);
    public static readonly Error PurchaseCompensated = new("investment.purchase_compensated", "Purchase could not be recorded; wallet payment was refunded.", 409);
    public static readonly Error CompensationFailed = new("investment.compensation_failed", "Purchase failed and its refund requires manual review.", 503);
}
