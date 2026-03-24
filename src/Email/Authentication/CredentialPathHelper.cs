namespace Notify.Console.Email.Authentication;

public static class CredentialPathHelper
{
    public static string GetCredentialDirectory(string profileName)
    {
        var baseDir = GetBaseDirectory();
        var profileDir = Path.Combine(baseDir, "gmail-credentials", profileName);

        if (!Directory.Exists(profileDir))
            Directory.CreateDirectory(profileDir);

        return profileDir;
    }

    public static string GetClientSecretPath(string profileName)
    {
        return Path.Combine(GetCredentialDirectory(profileName), "client_secret.json");
    }

    public static string GetUserCredentialPath(string profileName)
    {
        return Path.Combine(GetCredentialDirectory(profileName), "user_credential.json");
    }

    private static string GetBaseDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Notify.Console");
        }
        else
        {
            var home = Environment.GetEnvironmentVariable("HOME")
                       ?? throw new InvalidOperationException("HOME environment variable not set.");
            return Path.Combine(home, ".config", "notify-console");
        }
    }
}
