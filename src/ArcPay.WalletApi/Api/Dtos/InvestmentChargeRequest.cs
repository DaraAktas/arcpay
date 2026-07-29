namespace ArcPay.WalletApi.Api.Dtos;

public sealed record InvestmentChargeRequest(
    decimal Amount,
    string Currency,
    Guid TransactionRef,
    string? Description);
