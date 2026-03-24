using Notify.Console.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<SystemCommand>("system")
        .WithDescription("Show a native OS notification");

    config.AddCommand<SlackCommand>("slack")
        .WithDescription("Send a Slack message using a config profile");

    config.AddCommand<MessageBoxCommand>("messagebox")
        .WithDescription("Show a native OS message box dialog");

    config.AddCommand<EmailCommand>("email")
        .WithDescription("Send an email using a config profile");
});

return app.Run(args);
