using ScatGirl.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("scatgirl");

    config.AddCommand<FindDeclarationsCommand>("find")
        .WithDescription("Find all declarations of a named symbol in a C# codebase.")
        .WithExample(["find", ".", "IUserService"])
        .WithExample(["find", ".", "ProcessPayment", "--kind", "method"]);

    config.AddCommand<RefsCommand>("refs")
        .WithDescription("Find all references to a named symbol in a C# codebase.")
        .WithExample(["refs", ".", "AudioCaptureService"])
        .WithExample(["refs", ".", "IMonitoringStateService", "--kind", "identifier"])
        .WithExample(["refs", ".", "NoiseDetector", "--in-file", "**/*Service.cs"]);
});

return app.Run(args);
