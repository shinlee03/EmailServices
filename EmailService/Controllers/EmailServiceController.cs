using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;

namespace EmailService.Controllers;

[ApiController]
[Route("api")]
public class EmailServiceController : ControllerBase
{
    private readonly SmtpClient client;
    private readonly ILogger<EmailServiceController> logger;

    public EmailServiceController(SmtpClient _client, ILogger<EmailServiceController> _logger)
    {
        client = _client;
        logger = _logger;
    }

    private const string sender = "noreply@shinlee.org";
    
    [HttpPost]
    [Route("send")]
    public IActionResult Send([FromForm] EmailServiceRequest request)
    {
        var message = new MailMessage(sender, request.Recipient, request.Subject, request.Body);
        try
        {
            client.Send(message);
            logger.LogInformation($"Email sent to {request.Recipient} with subject {request.Subject} and Content {request.Body}.");
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Email sending failed with the following exception: {ex.Message}");
            return BadRequest();
        }
    }
}