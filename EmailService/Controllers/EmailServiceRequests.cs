namespace EmailService.Controllers;

public record EmailServiceRequest(string Recipient, string Subject, string Body);

public record EmailAuthenticateRequest(string Email);

public record CreateCookieRequest(string Email, Guid AuthenticationCode);

public record SendToShinRequest(string Subject, string Body);