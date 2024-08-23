using FluentValidation;

namespace EmailService.Controllers.Validators;

public class EmailServiceRequestValidator : AbstractValidator<EmailServiceRequest>
{
    public EmailServiceRequestValidator()
    {
        RuleFor(r => r.Body)
            .NotNull().WithMessage("Body is required.")
            .NotEmpty().WithMessage("Body cannot be empty.")
            .MaximumLength(100).WithMessage("Body cannot exceed 100 characters.");
        
        RuleFor(r => r.Recipient)
            .NotNull().WithMessage("Recipient is required.")
            .NotEmpty().WithMessage("Recipient cannot be empty.")
            .EmailAddress().WithMessage("Recipient must be a valid email address.");
        
        RuleFor(r => r.Subject)
            .NotNull().WithMessage("Subject is required.")
            .NotEmpty().WithMessage("Subject cannot be empty.")
            .MaximumLength(100).WithMessage("Subject cannot exceed 100 characters.");
    }
}