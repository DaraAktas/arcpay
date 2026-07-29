namespace ArcPay.WalletApi.Api.Dtos;

public sealed record InvestmentRefundRequest(Guid OriginalTransactionRef, Guid RefundTransactionRef);
