using ArcPay.Shared.Results;

namespace ArcPay.InvestmentApi.Application.Abstractions;

public interface IWalletPaymentGateway
{
    Task<Result<WalletPayment>> ChargeAsync(decimal amount, string currency, Guid reference, string description, CancellationToken cancellationToken);
    Task<Result<WalletPayment>> RefundAsync(Guid originalReference, Guid refundReference, CancellationToken cancellationToken);
}

public sealed record WalletPayment(Guid TransactionReference, decimal Amount, string Currency, bool IsReplay);
