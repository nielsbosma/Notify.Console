using System.Text.RegularExpressions;

namespace Notify.Console.Email;

public sealed class EmailMessage
{
    public required string From { get; init; }
    public required List<string> To { get; init; }
    public List<string> Cc { get; init; } = new();
    public List<string> Bcc { get; init; } = new();
    public required string Subject { get; init; }
    public string? BodyPlain { get; init; }
    public string? BodyHtml { get; init; }
    public List<EmailAttachment> Attachments { get; init; } = new();
    public bool IsDraft { get; init; }

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public void Validate()
    {
        ValidateEmail(From, "From");

        if (To.Count == 0)
            throw new InvalidOperationException("At least one recipient (To) is required.");

        foreach (var email in To)
            ValidateEmail(email, "To");

        foreach (var email in Cc)
            ValidateEmail(email, "Cc");

        foreach (var email in Bcc)
            ValidateEmail(email, "Bcc");

        if (string.IsNullOrWhiteSpace(Subject))
            throw new InvalidOperationException("Subject is required.");

        if (string.IsNullOrWhiteSpace(BodyPlain) && string.IsNullOrWhiteSpace(BodyHtml))
            throw new InvalidOperationException("Email body is required (either plain text or HTML).");

        foreach (var attachment in Attachments)
        {
            if (!File.Exists(attachment.FilePath))
                throw new FileNotFoundException($"Attachment file not found: {attachment.FilePath}");

            var fileInfo = new FileInfo(attachment.FilePath);
            if (fileInfo.Length > 25 * 1024 * 1024) // 25MB Gmail limit
                throw new InvalidOperationException(
                    $"Attachment '{attachment.FileName}' is too large ({fileInfo.Length / 1024 / 1024:F1} MB). Gmail limit is 25MB per message.");
        }
    }

    private static void ValidateEmail(string email, string field)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException($"{field} email address cannot be empty.");

        if (!EmailRegex.IsMatch(email))
            throw new InvalidOperationException($"Invalid email address in {field}: {email}");
    }
}

public sealed class EmailAttachment
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}
