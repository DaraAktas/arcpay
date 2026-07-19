using ArcPay.CustomerApi.Dtos;
using FluentValidation;
using System.Text;

namespace ArcPay.CustomerApi.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MaximumLength(72)
            .Must(password => Encoding.UTF8.GetByteCount(password) <= 72)
                .WithMessage("Password must be no more than 72 UTF-8 bytes.");
    }
}
