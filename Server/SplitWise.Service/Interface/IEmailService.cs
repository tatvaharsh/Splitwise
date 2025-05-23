namespace SplitWise.Service.Interface;

public interface IEmailService
{
    Task SendEmailAsync(List<string> recipientEmails, string subject, string body);

}
