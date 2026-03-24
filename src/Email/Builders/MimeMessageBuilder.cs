using System.Text;
using HtmlAgilityPack;

namespace Notify.Console.Email.Builders;

public static class MimeMessageBuilder
{
    public static string Build(EmailMessage message)
    {
        var sb = new StringBuilder();

        // Headers
        sb.AppendLine($"From: {message.From}");
        sb.AppendLine($"To: {string.Join(", ", message.To)}");

        if (message.Cc.Count > 0)
            sb.AppendLine($"Cc: {string.Join(", ", message.Cc)}");

        if (message.Bcc.Count > 0)
            sb.AppendLine($"Bcc: {string.Join(", ", message.Bcc)}");

        sb.AppendLine($"Subject: {message.Subject}");
        sb.AppendLine($"Date: {DateTime.UtcNow:R}");
        sb.AppendLine("MIME-Version: 1.0");

        var hasAttachments = message.Attachments.Count > 0;
        var hasHtml = !string.IsNullOrEmpty(message.BodyHtml);
        var hasPlain = !string.IsNullOrEmpty(message.BodyPlain);

        if (hasAttachments)
        {
            // multipart/mixed for attachments
            var outerBoundary = GenerateBoundary();
            sb.AppendLine($"Content-Type: multipart/mixed; boundary=\"{outerBoundary}\"");
            sb.AppendLine();

            // Body part
            sb.AppendLine($"--{outerBoundary}");

            if (hasHtml && hasPlain)
            {
                // multipart/alternative inside multipart/mixed
                var innerBoundary = GenerateBoundary();
                sb.AppendLine($"Content-Type: multipart/alternative; boundary=\"{innerBoundary}\"");
                sb.AppendLine();

                AppendPlainTextPart(sb, message.BodyPlain!, innerBoundary);
                AppendHtmlPart(sb, message.BodyHtml!, innerBoundary);

                sb.AppendLine($"--{innerBoundary}--");
            }
            else if (hasHtml)
            {
                AppendHtmlPart(sb, message.BodyHtml!, null);
            }
            else
            {
                AppendPlainTextPart(sb, message.BodyPlain!, null);
            }

            // Attachments
            foreach (var attachment in message.Attachments)
            {
                sb.AppendLine($"--{outerBoundary}");
                AppendAttachment(sb, attachment);
            }

            sb.AppendLine($"--{outerBoundary}--");
        }
        else if (hasHtml && hasPlain)
        {
            // multipart/alternative without attachments
            var boundary = GenerateBoundary();
            sb.AppendLine($"Content-Type: multipart/alternative; boundary=\"{boundary}\"");
            sb.AppendLine();

            AppendPlainTextPart(sb, message.BodyPlain!, boundary);
            AppendHtmlPart(sb, message.BodyHtml!, boundary);

            sb.AppendLine($"--{boundary}--");
        }
        else if (hasHtml)
        {
            // HTML only
            sb.AppendLine("Content-Type: text/html; charset=UTF-8");
            sb.AppendLine();
            sb.AppendLine(message.BodyHtml);
        }
        else
        {
            // Plain text only
            sb.AppendLine("Content-Type: text/plain; charset=UTF-8");
            sb.AppendLine();
            sb.AppendLine(message.BodyPlain);
        }

        return sb.ToString();
    }

    public static string ConvertHtmlToPlainText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var sb = new StringBuilder();
        ProcessNode(doc.DocumentNode, sb);

        return sb.ToString().Trim();
    }

    private static void ProcessNode(HtmlNode node, StringBuilder sb)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            var text = HtmlEntity.DeEntitize(node.InnerText);
            sb.Append(text);
        }
        else if (node.NodeType == HtmlNodeType.Element)
        {
            switch (node.Name.ToLower())
            {
                case "br":
                    sb.AppendLine();
                    break;
                case "p":
                case "div":
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    foreach (var child in node.ChildNodes)
                        ProcessNode(child, sb);
                    sb.AppendLine();
                    sb.AppendLine();
                    break;
                case "li":
                    sb.Append("• ");
                    foreach (var child in node.ChildNodes)
                        ProcessNode(child, sb);
                    sb.AppendLine();
                    break;
                case "script":
                case "style":
                    // Skip scripts and styles
                    break;
                default:
                    foreach (var child in node.ChildNodes)
                        ProcessNode(child, sb);
                    break;
            }
        }
        else
        {
            foreach (var child in node.ChildNodes)
                ProcessNode(child, sb);
        }
    }

    private static void AppendPlainTextPart(StringBuilder sb, string plainText, string? boundary)
    {
        if (boundary != null)
            sb.AppendLine($"--{boundary}");

        sb.AppendLine("Content-Type: text/plain; charset=UTF-8");
        sb.AppendLine();
        sb.AppendLine(plainText);
    }

    private static void AppendHtmlPart(StringBuilder sb, string html, string? boundary)
    {
        if (boundary != null)
            sb.AppendLine($"--{boundary}");

        sb.AppendLine("Content-Type: text/html; charset=UTF-8");
        sb.AppendLine();
        sb.AppendLine(html);
    }

    private static void AppendAttachment(StringBuilder sb, EmailAttachment attachment)
    {
        sb.AppendLine($"Content-Type: {attachment.ContentType}; name=\"{attachment.FileName}\"");
        sb.AppendLine($"Content-Disposition: attachment; filename=\"{attachment.FileName}\"");
        sb.AppendLine("Content-Transfer-Encoding: base64");
        sb.AppendLine();

        var fileBytes = File.ReadAllBytes(attachment.FilePath);
        var base64 = Convert.ToBase64String(fileBytes);

        // Split into 76-character lines as per RFC 2045
        for (int i = 0; i < base64.Length; i += 76)
        {
            var length = Math.Min(76, base64.Length - i);
            sb.AppendLine(base64.Substring(i, length));
        }
    }

    private static string GenerateBoundary()
    {
        return $"boundary_{Guid.NewGuid():N}";
    }
}
