namespace ArcPay.WalletApi.Application.Transactions;

public sealed record TransactionHistoryView(
    Guid TransactionRef,
    string Type,
    string Direction,
    decimal Amount,
    string Currency,
    string Status,
    string? CounterpartyCustomerNumber,
    string? Description,
    DateTime CreatedAt);
