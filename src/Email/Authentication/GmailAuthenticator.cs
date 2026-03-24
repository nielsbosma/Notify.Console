using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace Notify.Console.Email.Authentication;

public static class GmailAuthenticator
{
    public static async Task<GmailService> GetAuthenticatedServiceAsync(string profileName, CancellationToken cancellation = default)
    {
        var clientSecretPath = CredentialPathHelper.GetClientSecretPath(profileName);

        if (!File.Exists(clientSecretPath))
        {
            var credDir = CredentialPathHelper.GetCredentialDirectory(profileName);
            throw new InvalidOperationException(
                $"Gmail credentials not found for profile '{profileName}'.\n\n" +
                "Setup instructions:\n" +
                "1. Create Google Cloud project: https://console.cloud.google.com\n" +
                "2. Enable Gmail API\n" +
                "3. Create OAuth 2.0 credentials (Desktop app)\n" +
                "4. Download client_secret.json\n" +
                $"5. Place it at: {clientSecretPath}");
        }

        UserCredential credential;
        try
        {
            using var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read);

            var credentialDir = CredentialPathHelper.GetCredentialDirectory(profileName);

            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { GmailService.Scope.GmailSend },
                "user",
                cancellation,
                new FileDataStore(credentialDir, true));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to authenticate with Gmail for profile '{profileName}'. " +
                "Please verify your client_secret.json is valid and try again.", ex);
        }

        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Notify.Console"
        });
    }
}
