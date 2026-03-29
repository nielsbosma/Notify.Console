using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        public string? Message { get; init; }

        [CommandOption("--json <JSON>")]
        public string? Json { get; init; }

        [CommandOption("--json-file <PATH>")]
        public string? JsonFile { get; init; }

        public override ValidationResult Validate()
        {
            var modes = new[] { Message, Json, JsonFile }.Count(x => x is not null);

            if (modes == 0)
                return ValidationResult.Error("Specify one of --message, --json, or --json-file.");
            if (modes > 1)
                return ValidationResult.Error("Only one of --message, --json, or --json-file can be used.");

            return ValidationResult.Success();
        }
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

            if (settings.Message is not null)
            {
                await SendTextMessage(profile, settings.Message);
            }
            else
            {
                var json = settings.Json ?? await File.ReadAllTextAsync(settings.JsonFile!, cancellation);
                await SendJsonMessage(profile, json);
            }

            AnsiConsole.MarkupLine($"[green]Slack message sent via profile '{Markup.Escape(settings.Profile)}'.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to send Slack message:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static async Task SendTextMessage(SlackProfile profile, string message)
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

    private static async Task SendJsonMessage(SlackProfile profile, string json)
    {
        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException("Invalid JSON: parsed to null.");

        if (node is not JsonObject obj)
            throw new InvalidOperationException("JSON payload must be an object.");

        if (profile.Channel is not null)
            obj.TryAdd("channel", profile.Channel);
        if (profile.Username is not null)
            obj.TryAdd("username", profile.Username);
        if (profile.IconEmoji is not null)
            obj.TryAdd("icon_emoji", profile.IconEmoji);

        using var client = new HttpClient();
        var content = new StringContent(obj.ToJsonString(), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(profile.WebhookUrl, content);

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
