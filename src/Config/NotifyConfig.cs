namespace Notify.Console.Config;

public sealed class NotifyConfig
{
    public Dictionary<string, SlackProfile> Slack { get; set; } = new();
    public Dictionary<string, EmailProfile> Email { get; set; } = new();
}

public sealed class SlackProfile
{
    public required string WebhookUrl { get; set; }
    public string? Channel { get; set; }
    public string? Username { get; set; }
    public string? IconEmoji { get; set; }
}

public sealed class EmailProfile
{
    public required string Provider { get; set; }
    public required string From { get; set; }
    public string? DefaultTo { get; set; }
    public List<string>? DefaultCc { get; set; }
    public List<string>? DefaultBcc { get; set; }
}
