using ArcPay.WalletApi.Application.Wallets;

namespace ArcPay.WalletApi.Api.Dtos;

public sealed record WalletResponse(int Id, string CustomerNumber, decimal Balance, string Currency)
{
    public static WalletResponse From(WalletView wallet) => new(
        wallet.Id,
        wallet.CustomerNumber,
        wallet.Balance,
        wallet.Currency);
}
