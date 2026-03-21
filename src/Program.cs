using Notify.Console.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<SystemCommand>("system")
        .WithDescription("Show a native OS notification");

    config.AddCommand<SlackCommand>("slack")
        .WithDescription("Send a Slack message using a config profile");
});

return app.Run(args);
