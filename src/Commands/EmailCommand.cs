using System.Text.RegularExpressions;
using Notify.Console.Config;
using Notify.Console.Email;
using Notify.Console.Email.Builders;
using Notify.Console.Email.Providers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Notify.Console.Commands;

public sealed class EmailCommand : AsyncCommand<EmailCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<PROFILE>")]
        public required string Profile { get; init; }

        [CommandOption("--to <EMAIL>")]
        public string[]? To { get; init; }

        [CommandOption("--cc <EMAIL>")]
        public string[]? Cc { get; init; }

        [CommandOption("--bcc <EMAIL>")]
        public string[]? Bcc { get; init; }

        [CommandOption("--subject <SUBJECT>")]
        public required string Subject { get; init; }

        [CommandOption("--body <BODY>")]
        public string? Body { get; init; }

        [CommandOption("--file <FILE>")]
        public string? File { get; init; }

        [CommandOption("--attach <FILE>")]
        public string[]? Attachments { get; init; }

        [CommandOption("--draft")]
        public bool Draft { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation = default)
    {
        try
        {
            var config = ConfigLoader.Load();

            if (!config.Email.TryGetValue(settings.Profile, out var profile))
            {
                var available = config.Email.Keys.Any()
                    ? string.Join(", ", config.Email.Keys)
                    : "(none)";
                throw new InvalidOperationException(
                    $"Email profile '{settings.Profile}' not found. Available profiles: {available}");
            }

            // Read body content (priority: stdin > --body > --file)
            string? bodyContent = null;

            if (System.Console.IsInputRedirected)
            {
                using var stdin = System.Console.OpenStandardInput();
                using var reader = new StreamReader(stdin);
                bodyContent = await reader.ReadToEndAsync(cancellation);
            }
            else if (settings.Body is not null)
            {
                bodyContent = settings.Body;
            }
            else if (settings.File is not null)
            {
                if (!System.IO.File.Exists(settings.File))
                    throw new FileNotFoundException($"File not found: {settings.File}");
                bodyContent = await System.IO.File.ReadAllTextAsync(settings.File, cancellation);
            }
            else
            {
                throw new InvalidOperationException("No email body provided. Use stdin, --body, or --file.");
            }

            // Auto-detect HTML
            var isHtml = IsHtmlContent(bodyContent);

            // Build recipient lists (expand comma-separated and combine with defaults)
            var toList = ExpandRecipients(settings.To);
            if (toList.Count == 0 && profile.DefaultTo != null)
                toList.Add(profile.DefaultTo);

            if (toList.Count == 0)
                throw new InvalidOperationException("At least one recipient (--to) is required.");

            var ccList = ExpandRecipients(settings.Cc);
            if (profile.DefaultCc != null)
                ccList.AddRange(profile.DefaultCc);

            var bccList = ExpandRecipients(settings.Bcc);
            if (profile.DefaultBcc != null)
                bccList.AddRange(profile.DefaultBcc);

            // Build attachments list
            var attachments = new List<EmailAttachment>();
            if (settings.Attachments != null)
            {
                foreach (var attachmentPath in settings.Attachments)
                {
                    if (!System.IO.File.Exists(attachmentPath))
                        throw new FileNotFoundException($"Attachment not found: {attachmentPath}");

                    var fileName = Path.GetFileName(attachmentPath);
                    var contentType = GetMimeType(attachmentPath);

                    attachments.Add(new EmailAttachment
                    {
                        FilePath = attachmentPath,
                        FileName = fileName,
                        ContentType = contentType
                    });
                }
            }

            // Build email message
            var message = new EmailMessage
            {
                From = profile.From,
                To = toList,
                Cc = ccList,
                Bcc = bccList,
                Subject = settings.Subject,
                BodyHtml = isHtml ? bodyContent : null,
                BodyPlain = isHtml ? MimeMessageBuilder.ConvertHtmlToPlainText(bodyContent) : bodyContent,
                Attachments = attachments,
                IsDraft = settings.Draft
            };

            // Send email
            IEmailProvider provider = profile.Provider.ToLower() switch
            {
                "gmail" => new GmailEmailProvider(settings.Profile),
                _ => throw new InvalidOperationException($"Unknown email provider: {profile.Provider}")
            };

            await provider.SendEmailAsync(message, cancellation);

            var action = settings.Draft ? "Draft created" : "Email sent";
            var recipients = string.Join(", ", toList.Select(Markup.Escape));
            AnsiConsole.MarkupLine($"[green]{action} via profile '{Markup.Escape(settings.Profile)}'[/]");
            AnsiConsole.MarkupLine($"  To: {recipients}");
            AnsiConsole.MarkupLine($"  Subject: {Markup.Escape(settings.Subject)}");

            if (attachments.Count > 0)
                AnsiConsole.MarkupLine($"  Attachments: {attachments.Count}");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to send email:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static List<string> ExpandRecipients(string[]? recipients)
    {
        if (recipients == null || recipients.Length == 0)
            return new List<string>();

        var expanded = new List<string>();
        foreach (var recipient in recipients)
        {
            // Support comma-separated lists
            var addresses = recipient.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            expanded.AddRange(addresses);
        }

        return expanded;
    }

    private static bool IsHtmlContent(string content)
    {
        // Check for common HTML tags
        var htmlPattern = @"<(html|body|div|p|span|h[1-6]|table|ul|ol|li|br|img|a)\b";
        return Regex.IsMatch(content, htmlPattern, RegexOptions.IgnoreCase);
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".html" or ".htm" => "text/html",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".zip" => "application/zip",
            ".tar" => "application/x-tar",
            ".gz" => "application/gzip",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".mp3" => "audio/mpeg",
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            _ => "application/octet-stream"
        };
    }
}
