namespace ArcPay.WalletApi.Api.Dtos;

public sealed record InvestmentPaymentResponse(
    Guid TransactionRef,
    decimal Amount,
    string Currency,
    bool IsReplay);
