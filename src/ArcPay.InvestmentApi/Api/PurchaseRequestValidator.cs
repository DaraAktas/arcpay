using FluentValidation;

namespace ArcPay.InvestmentApi.Api;

public sealed class PurchaseRequestValidator : AbstractValidator<PurchaseRequest>
{
    public PurchaseRequestValidator()
    {
        RuleFor(request => request.Symbol).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9.]{1,20}$");
        RuleFor(request => request.Quantity).GreaterThan(0).PrecisionScale(18, 8, false);
        RuleFor(request => request.PurchaseRef).NotEmpty();
    }
}
