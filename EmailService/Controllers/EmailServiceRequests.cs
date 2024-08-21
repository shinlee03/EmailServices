namespace EmailService.Controllers;

public record EmailServiceRequest(string Recipient, string Subject, string Body);