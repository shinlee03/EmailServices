using System.Net.Mail;

namespace EmailService.Smtp;

public interface ISmtpClient
{
    public Task SendMailAsync(MailMessage message);
    
}