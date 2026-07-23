using ArcPay.WalletApi.Api.Dtos;
using FluentValidation;

namespace ArcPay.WalletApi.Api.Validators;

public sealed class OpenWalletRequestValidator : AbstractValidator<OpenWalletRequest>
{
    public OpenWalletRequestValidator()
    {
        RuleFor(request => request.Currency)
            .NotEmpty()
            .Length(3);
    }
}
