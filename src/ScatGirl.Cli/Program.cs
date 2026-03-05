using ScatGirl.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("scatgirl")
        .SetApplicationVersion(ThisAssembly.AssemblyInformationalVersion);

    config.AddCommand<FindDeclarationsCommand>("find")
        .WithDescription("Find all declarations of a named symbol in a C# codebase.")
        .WithExample(["find", ".", "IUserService"])
        .WithExample(["find", ".", "ProcessPayment", "--kind", "method"]);

    config.AddCommand<RefsCommand>("refs")
        .WithDescription("Find all references to a named symbol in a C# codebase.")
        .WithExample(["refs", ".", "AudioCaptureService"])
        .WithExample(["refs", ".", "IMonitoringStateService", "--kind", "identifier"])
        .WithExample(["refs", ".", "NoiseDetector", "--in-file", "**/*Service.cs"]);

    config.AddCommand<MembersCommand>("members")
        .WithDescription("List all members of a type declared in a C# codebase.")
        .WithExample(["members", ".", "AudioCaptureService"])
        .WithExample(["members", ".", "AudioCaptureService", "--kind", "method"])
        .WithExample(["members", ".", "SyntaxNavigator", "--in-file", "**/ScatGirl.Core/**"]);

    config.AddCommand<MetaCommand>("meta")
        .WithDescription("Show metadata about the ScatGirl CLI tool, including version and build information.");
});

return app.Run(args);
