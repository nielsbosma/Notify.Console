using Notify.Console.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<SystemCommand>("system")
        .WithDescription("Show a native OS notification");
});

return app.Run(args);
