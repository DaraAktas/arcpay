using ArcPay.WalletApi.Application.Transactions;

namespace ArcPay.WalletApi.Api.Dtos;

public sealed record TransactionHistoryResponse(
    Guid TransactionRef,
    string Type,
    string Direction,
    decimal Amount,
    string Currency,
    string Status,
    string? CounterpartyCustomerNumber,
    string? Description,
    DateTime CreatedAt)
{
    public static TransactionHistoryResponse From(TransactionHistoryView view) => new(
        view.TransactionRef,
        view.Type,
        view.Direction,
        view.Amount,
        view.Currency,
        view.Status,
        view.CounterpartyCustomerNumber,
        view.Description,
        view.CreatedAt);
}
