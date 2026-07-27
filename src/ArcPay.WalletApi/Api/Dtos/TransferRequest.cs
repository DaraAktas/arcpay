namespace ArcPay.WalletApi.Api.Dtos;

public sealed record TransferRequest(
    string ToCustomerNumber,
    decimal Amount,
    string Currency,
    Guid TransactionRef,
    string? Description);
