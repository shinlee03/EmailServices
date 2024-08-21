using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;

namespace EmailService.Controllers;

[ApiController]
[Route("api")]
public class EmailServiceController : ControllerBase
{
    private readonly SmtpClient client;

    public EmailServiceController(SmtpClient _client)
    {
        client = _client;
    }
    
    [HttpPost]
    [Route("send")]
    public async Task Send()
    {
        string sender = "donotreply@shinlee.org";
        string recipient = "shinlee@umich.edu";
        string subject = "Welcome to Azure Communication Service Email SMTP";
        string body = "This email message is sent from Azure Communication Service Email using SMTP.";
    
        var message = new MailMessage(sender, recipient, subject, body);
        try
        {
            client.Send(message);
            Console.WriteLine("The email was successfully sent using Smtp.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Smtp send failed with the exception: {ex.Message}.");
        }
    }
}