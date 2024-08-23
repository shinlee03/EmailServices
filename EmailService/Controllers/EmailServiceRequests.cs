using FluentValidation;

namespace EmailService.Controllers;

public record EmailServiceRequest(string Recipient, string Subject, string Body);
public record CreateCookieRequest(string Email, Guid AuthenticationCode);
public record SendToShinRequest(string Subject, string Body);
