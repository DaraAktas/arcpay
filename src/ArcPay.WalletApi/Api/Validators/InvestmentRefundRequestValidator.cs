using ArcPay.WalletApi.Api.Dtos;
using FluentValidation;

namespace ArcPay.WalletApi.Api.Validators;

public sealed class InvestmentRefundRequestValidator : AbstractValidator<InvestmentRefundRequest>
{
    public InvestmentRefundRequestValidator()
    {
        RuleFor(request => request.OriginalTransactionRef).NotEmpty();
        RuleFor(request => request.RefundTransactionRef).NotEmpty();
        RuleFor(request => request).Must(request => request.OriginalTransactionRef != request.RefundTransactionRef)
            .WithMessage("Refund reference must differ from the original transaction reference.");
    }
}
