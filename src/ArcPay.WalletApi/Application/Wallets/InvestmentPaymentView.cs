namespace ArcPay.WalletApi.Application.Wallets;

public sealed record InvestmentPaymentView(
    Guid TransactionReference,
    decimal Amount,
    string Currency,
    bool IsReplay);
