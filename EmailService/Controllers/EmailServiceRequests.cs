namespace EmailService.Controllers;

public record EmailServiceRequest(string Recipient, Guid AuthenticationCode, string Subject, string Body);

public record EmailAuthenticateRequest(string Email);

public record CreateCookieRequest(string Email, Guid AuthenticationCode);