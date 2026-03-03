using System.ComponentModel;
using System.Text.Json;
using Spectre.Console.Cli;

namespace ScatGirl.Cli;

class BaseSettings : CommandSettings
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [CommandArgument(0, "<root>")]
    [Description("Path to the repository root")]
    public string Root { get; init; } = "";

    [CommandOption("--json")]
    [Description("Output results as JSON instead of formatted text")]
    public bool Json { get; init; }
}
