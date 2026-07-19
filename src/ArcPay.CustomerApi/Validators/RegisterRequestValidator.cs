using ArcPay.CustomerApi.Dtos;
using FluentValidation;
using System.Text;

namespace ArcPay.CustomerApi.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Email)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(72)
            .Must(password => Encoding.UTF8.GetByteCount(password) <= 72)
                .WithMessage("Password must be no more than 72 UTF-8 bytes.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.");
    }
}
