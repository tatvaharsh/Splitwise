using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SplitWise.Domain.Generic.Entity;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class EmailService(IOptions<EmailSettings> emailSettings) : IEmailService
{
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    public async Task SendEmailAsync(List<string> recipientEmails, string subject, string body)
    {
        SmtpClient smtpClient = new(_emailSettings.EmailProvider, _emailSettings.Port)
        {
            Credentials = new NetworkCredential(_emailSettings.EmailAddress, _emailSettings.Password),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        MailMessage mailMessage = new()
        {
            From = new MailAddress(_emailSettings.EmailAddress),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        foreach (string to in recipientEmails)
        {
            mailMessage.To.Add(to);
        }

        await smtpClient.SendMailAsync(mailMessage);
    }
}
