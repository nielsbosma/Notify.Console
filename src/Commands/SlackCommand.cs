using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Notify.Console.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Notify.Console.Commands;

public sealed class SlackCommand : AsyncCommand<SlackCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<PROFILE>")]
        public required string Profile { get; init; }

        [CommandOption("--message <MESSAGE>")]
        public required string Message { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation = default)
    {
        try
        {
            var config = ConfigLoader.Load();

            if (!config.Slack.TryGetValue(settings.Profile, out var profile))
            {
                var available = config.Slack.Keys.Any()
                    ? string.Join(", ", config.Slack.Keys)
                    : "(none)";
                throw new InvalidOperationException(
                    $"Slack profile '{settings.Profile}' not found. Available profiles: {available}");
            }

            await SendMessage(profile, settings.Message);

            AnsiConsole.MarkupLine($"[green]Slack message sent via profile '{Markup.Escape(settings.Profile)}'.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to send Slack message:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static async Task SendMessage(SlackProfile profile, string message)
    {
        var payload = new SlackPayload { Text = message };

        if (profile.Channel is not null)
            payload.Channel = profile.Channel;
        if (profile.Username is not null)
            payload.Username = profile.Username;
        if (profile.IconEmoji is not null)
            payload.IconEmoji = profile.IconEmoji;

        using var client = new HttpClient();
        var response = await client.PostAsJsonAsync(profile.WebhookUrl, payload);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Slack API returned {response.StatusCode}: {body}");
        }
    }

    private sealed class SlackPayload
    {
        [JsonPropertyName("text")]
        public required string Text { get; set; }

        [JsonPropertyName("channel")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Channel { get; set; }

        [JsonPropertyName("username")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Username { get; set; }

        [JsonPropertyName("icon_emoji")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IconEmoji { get; set; }
    }
}
