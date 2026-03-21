namespace Notify.Console.Config;

public sealed class NotifyConfig
{
    public Dictionary<string, SlackProfile> Slack { get; set; } = new();
}

public sealed class SlackProfile
{
    public required string WebhookUrl { get; set; }
    public string? Channel { get; set; }
    public string? Username { get; set; }
    public string? IconEmoji { get; set; }
}
