using ArcPay.WalletApi.Api.Dtos;
using FluentValidation;

namespace ArcPay.WalletApi.Api.Validators;

public sealed class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(request => request.ToCustomerNumber)
            .NotEmpty()
            .Matches("^ARC-[0-9]{10}$");
        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .PrecisionScale(18, 8, true);
        RuleFor(request => request.Currency)
            .NotEmpty()
            .MaximumLength(3);
        RuleFor(request => request.TransactionRef).NotEmpty();
        RuleFor(request => request.Description).MaximumLength(500);
    }
}
