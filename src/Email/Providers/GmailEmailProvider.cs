using System.Text;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Notify.Console.Email.Authentication;
using Notify.Console.Email.Builders;

namespace Notify.Console.Email.Providers;

public sealed class GmailEmailProvider : IEmailProvider
{
    private readonly string _profileName;

    public GmailEmailProvider(string profileName)
    {
        _profileName = profileName;
    }

    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellation = default)
    {
        message.Validate();

        var service = await GmailAuthenticator.GetAuthenticatedServiceAsync(_profileName, cancellation);

        var mimeMessage = MimeMessageBuilder.Build(message);
        var rawMessage = EncodeBase64Url(mimeMessage);

        var gmailMessage = new Message { Raw = rawMessage };

        try
        {
            if (message.IsDraft)
            {
                var draft = new Draft { Message = gmailMessage };
                await service.Users.Drafts.Create(draft, "me").ExecuteAsync(cancellation);
            }
            else
            {
                await service.Users.Messages.Send(gmailMessage, "me").ExecuteAsync(cancellation);
            }
        }
        catch (Google.GoogleApiException ex)
        {
            throw new InvalidOperationException(
                $"Gmail API error: {ex.Message}. " +
                "Check your internet connection, OAuth credentials, and Gmail API quota.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to send email via Gmail: {ex.Message}", ex);
        }
    }

    private static string EncodeBase64Url(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var base64 = Convert.ToBase64String(bytes);

        // Convert to Base64url format (RFC 4648)
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
