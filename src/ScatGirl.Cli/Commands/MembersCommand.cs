using ScatGirl.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ScatGirl.Cli.Commands;

sealed class MembersCommand : Command<MembersCommand.Settings>
{
    static readonly string[] KindOrder = ["field", "property", "constructor", "method", "event"];

    public sealed class Settings : BaseSettings
    {
        [CommandArgument(1, "<symbol>")]
        [Description("Type name to inspect (e.g. AudioCaptureService)")]
        public string Symbol { get; init; } = "";

        [CommandOption("-k|--kind")]
        [Description("Filter by member kind: method, property, field, constructor, event")]
        public string? Kind { get; init; }

        [CommandOption("--in-file")]
        [Description("Restrict search to files matching glob (e.g. \"**/*Service.cs\")")]
        public string? InFile { get; init; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken ct)
    {
        var members = new SyntaxNavigator().FindMembers(
            settings.Root, settings.Symbol, settings.Kind, settings.InFile);

        if (settings.Json)
        {
            PrintJson(members, settings);
        }
        else
        {
            PrintFormatted(members, settings);
        }

        return 0;
    }

    static void PrintJson(IReadOnlyList<TypeMember> members, Settings settings)
    {
        var result = new
        {
            root       = settings.Root,
            typeName   = settings.Symbol,
            kind       = settings.Kind,
            inFile     = settings.InFile,
            count      = members.Count,
            results    = members.GroupBy(m => m.Location.FilePath)
                        .Select(g => new {
                            file = g.Key,
                            hits = g.Select(m => new {
                                kind = m.Kind,
                                sig  = m.Signature,
                                line = m.Location.Line
                            })
                        })
        };
        Console.WriteLine(JsonSerializer.Serialize(result, BaseSettings.JsonOptions));
    }

    static void PrintFormatted(IReadOnlyList<TypeMember> members, Settings settings)
    {
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(settings.Symbol)}[/] — {members.Count} member(s)\n");

        if (members.Count == 0)
        {
            AnsiConsole.MarkupLine("  [yellow](no members found)[/]");
            return;
        }

        foreach (var fileGroup in members.GroupBy(m => m.Location.FilePath))
        {
            AnsiConsole.MarkupLine($"[blue]{Markup.Escape(fileGroup.Key)}[/]");

            var kindGroups = fileGroup
                .GroupBy(m => m.Kind)
                .OrderBy(g => Array.IndexOf(KindOrder, g.Key) is var i && i >= 0 ? i : int.MaxValue);

            foreach (var group in kindGroups)
            {
                AnsiConsole.MarkupLine($"  [cyan]{group.Key}s[/]");

                var table = new Table().Border(TableBorder.None);
                table.AddColumn(new TableColumn("[bold yellow]Signature[/]"));
                table.AddColumn(new TableColumn("[bold yellow]Line[/]"));

                foreach (var m in group)
                {
                    table.AddRow($"    {Markup.Escape(m.Signature)}", $"[bold]{m.Location.Line}[/]");
                }

                AnsiConsole.Write(table);
            }
            AnsiConsole.WriteLine();
        }
    }
}
