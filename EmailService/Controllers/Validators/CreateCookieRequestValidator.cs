using FluentValidation;

namespace EmailService.Controllers.Validators;

public class CreateCookieRequestValidator : AbstractValidator<CreateCookieRequest>
{
    public CreateCookieRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotNull().WithMessage("Email is required.")
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(r => r.AuthenticationCode)
            .NotNull().WithMessage("Authentication code is required.")
            .NotEmpty().WithMessage("Authentication code is required.");
    }
}