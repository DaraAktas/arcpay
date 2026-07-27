using ArcPay.WalletApi.Application.Wallets;

namespace ArcPay.WalletApi.Application.Transactions;

public sealed record TransferView(
    Guid TransactionRef,
    string ReceiverCustomerNumber,
    decimal Amount,
    string Currency,
    WalletView SenderWallet,
    DateTime CreatedAt,
    bool IsReplay);
