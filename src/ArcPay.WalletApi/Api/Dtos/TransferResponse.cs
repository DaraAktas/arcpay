using ArcPay.WalletApi.Application.Transactions;

namespace ArcPay.WalletApi.Api.Dtos;

public sealed record TransferResponse(
    Guid TransactionRef,
    string ReceiverCustomerNumber,
    decimal Amount,
    string Currency,
    WalletResponse SenderWallet,
    DateTime CreatedAt,
    bool IsReplay)
{
    public static TransferResponse From(TransferView view) => new(
        view.TransactionRef,
        view.ReceiverCustomerNumber,
        view.Amount,
        view.Currency,
        WalletResponse.From(view.SenderWallet),
        view.CreatedAt,
        view.IsReplay);
}
