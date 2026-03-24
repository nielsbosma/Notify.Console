namespace Notify.Console.Email;

public interface IEmailProvider
{
    Task SendEmailAsync(EmailMessage message, CancellationToken cancellation = default);
}
