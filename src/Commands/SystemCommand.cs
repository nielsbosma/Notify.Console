using System.Diagnostics;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Notify.Console.Commands;

public sealed class SystemCommand : Command<SystemCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--title <TITLE>")]
        public required string Title { get; init; }

        [CommandOption("--description <DESCRIPTION>")]
        public required string Description { get; init; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation = default)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                ShowWindows(settings.Title, settings.Description);
            else if (OperatingSystem.IsMacOS())
                ShowMacOs(settings.Title, settings.Description);
            else if (OperatingSystem.IsLinux())
                ShowLinux(settings.Title, settings.Description);
            else
                throw new PlatformNotSupportedException("Notifications are not supported on this platform.");

            AnsiConsole.MarkupLine("[green]Notification sent.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to send notification:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static void ShowWindows(string title, string description)
    {
        var script = $"""
            Add-Type -AssemblyName System.Windows.Forms
            $n = New-Object System.Windows.Forms.NotifyIcon
            $n.Icon = [System.Drawing.SystemIcons]::Information
            $n.BalloonTipTitle = '{title.Replace("'", "''")}'
            $n.BalloonTipText = '{description.Replace("'", "''")}'
            $n.Visible = $true
            $n.ShowBalloonTip(5000)
            Start-Sleep -Milliseconds 100
            """;

        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        Run("powershell", $"-NoProfile -EncodedCommand {encoded}");
    }

    private static void ShowMacOs(string title, string description)
    {
        var escaped = description.Replace("\"", "\\\"");
        var titleEscaped = title.Replace("\"", "\\\"");

        Run("osascript", $"-e \"display notification \\\"{escaped}\\\" with title \\\"{titleEscaped}\\\"\"");
    }

    private static void ShowLinux(string title, string description)
    {
        Run("notify-send", $"\"{title}\" \"{description}\"");
    }

    private static void Run(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        process?.WaitForExit();

        if (process is { ExitCode: not 0 })
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Notification process exited with code {process.ExitCode}: {error}");
        }
    }
}
