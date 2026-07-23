using ArcPay.WalletApi.Api.Dtos;
using FluentValidation;

namespace ArcPay.WalletApi.Api.Validators;

public sealed class DepositRequestValidator : AbstractValidator<DepositRequest>
{
    public DepositRequestValidator()
    {
        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .PrecisionScale(18, 8, true);

        RuleFor(request => request.TransactionRef).NotEmpty();
    }
}
