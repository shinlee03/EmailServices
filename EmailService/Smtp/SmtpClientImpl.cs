using System.Net.Mail;

namespace EmailService.Smtp;

public class SmtpClientImpl : ISmtpClient
{
    private readonly SmtpClient smtpClient;

    public SmtpClientImpl(SmtpClient smtpClient)
    {
        this.smtpClient = smtpClient;
    }
    public async Task SendMailAsync(MailMessage message)
    {
        await smtpClient.SendMailAsync(message);
    }
}