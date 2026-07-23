namespace ArcPay.WalletApi.Application.Wallets;

public sealed record DepositView(Guid TransactionRef, WalletView Wallet);
