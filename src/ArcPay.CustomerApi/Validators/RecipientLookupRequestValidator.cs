using ArcPay.CustomerApi.Dtos;
using FluentValidation;

namespace ArcPay.CustomerApi.Validators;

public sealed class RecipientLookupRequestValidator : AbstractValidator<RecipientLookupRequest>
{
    public RecipientLookupRequestValidator() => RuleFor(request => request.Identifier)
        .NotEmpty()
        .MaximumLength(320);
}
