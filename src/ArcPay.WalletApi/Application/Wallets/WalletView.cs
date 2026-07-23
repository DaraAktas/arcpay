using ArcPay.WalletApi.Domain.Wallets;

namespace ArcPay.WalletApi.Application.Wallets;

public sealed record WalletView(int Id, string CustomerNumber, decimal Balance, string Currency)
{
    public static WalletView From(Wallet wallet) => new(
        wallet.Id,
        wallet.CustomerNumber.Value,
        wallet.Balance.Amount,
        wallet.Currency.Code);
}
