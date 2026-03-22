using System.Diagnostics;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Notify.Console.Commands;

public sealed class MessageBoxCommand : Command<MessageBoxCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--title <TITLE>")]
        public required string Title { get; init; }

        [CommandOption("--message <MESSAGE>")]
        public required string Message { get; init; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation = default)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                ShowWindows(settings.Title, settings.Message);
            else if (OperatingSystem.IsMacOS())
                ShowMacOs(settings.Title, settings.Message);
            else if (OperatingSystem.IsLinux())
                ShowLinux(settings.Title, settings.Message);
            else
                throw new PlatformNotSupportedException("Message boxes are not supported on this platform.");

            AnsiConsole.MarkupLine("[green]Message box shown.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to show message box:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static void ShowWindows(string title, string message)
    {
        var script = $"""
            Add-Type -AssemblyName System.Windows.Forms
            [System.Windows.Forms.MessageBox]::Show('{message.Replace("'", "''")}', '{title.Replace("'", "''")}', 'OK', 'Information')
            """;

        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        Run("powershell", $"-NoProfile -EncodedCommand {encoded}");
    }

    private static void ShowMacOs(string title, string message)
    {
        var titleEscaped = title.Replace("\"", "\\\"");
        var messageEscaped = message.Replace("\"", "\\\"");

        Run("osascript", $"-e \"display dialog \\\"{messageEscaped}\\\" with title \\\"{titleEscaped}\\\" buttons {{\\\"OK\\\"}} default button \\\"OK\\\"\"");
    }

    private static void ShowLinux(string title, string message)
    {
        Run("zenity", $"--info --title=\"{title}\" --text=\"{message}\"");
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
            throw new InvalidOperationException($"Message box process exited with code {process.ExitCode}: {error}");
        }
    }
}
