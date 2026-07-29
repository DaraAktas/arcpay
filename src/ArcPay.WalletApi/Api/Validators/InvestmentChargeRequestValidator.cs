using ArcPay.WalletApi.Api.Dtos;
using FluentValidation;

namespace ArcPay.WalletApi.Api.Validators;

public sealed class InvestmentChargeRequestValidator : AbstractValidator<InvestmentChargeRequest>
{
    public InvestmentChargeRequestValidator()
    {
        RuleFor(request => request.Amount).GreaterThan(0).PrecisionScale(18, 8, false);
        RuleFor(request => request.Currency).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
        RuleFor(request => request.TransactionRef).NotEmpty();
        RuleFor(request => request.Description).MaximumLength(500);
    }
}
