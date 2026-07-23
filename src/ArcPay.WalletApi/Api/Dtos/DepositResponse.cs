using ArcPay.WalletApi.Application.Wallets;

namespace ArcPay.WalletApi.Api.Dtos;

public sealed record DepositResponse(Guid TransactionRef, WalletResponse Wallet)
{
    public static DepositResponse From(DepositView deposit) =>
        new(deposit.TransactionRef, WalletResponse.From(deposit.Wallet));
}
