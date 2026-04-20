using ScatGirl.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ScatGirl.Cli.Commands;

sealed class RefsCommand : Command<RefsCommand.Settings>
{
    public sealed class Settings : BaseSettings
    {
        [CommandArgument(1, "<symbol>")]
        [Description("Symbol name to find references to")]
        public string Symbol { get; init; } = "";

        [CommandOption("-k|--kind")]
        [Description("Filter by reference kind: identifier, typeof, nameof, attribute, " +
                     "implementation, invocation, object-creation, type-argument")]
        public string? Kind { get; init; }

        [CommandOption("--in-file")]
        [Description("Restrict search to files matching glob (e.g. \"**/*Service.cs\")")]
        public string? InFile { get; init; }

        [CommandOption("--regex")]
        [Description("Interpret <symbol> as a regular expression for pattern-based search.")]
        public bool Regex { get; init; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken ct)
    {
        var refs = new SyntaxNavigator().FindReferences(
            settings.Root, settings.Symbol, settings.Kind, settings.InFile, settings.Regex);

        if (settings.Json) PrintJson(refs, settings);
        else PrintFormatted(refs, settings);

        return 0;
    }

    static void PrintJson(IReadOnlyList<SymbolReference> refs, Settings settings)
    {
        var result = new
        {
            root       = settings.Root,
            symbolName = settings.Symbol,
            kind       = settings.Kind,
            inFile     = settings.InFile,
            analysis   = "syntactic",
            count      = refs.Count,
            references = refs.Select(r => new
            {
                r.FilePath,
                r.Line,
                r.Column,
                r.LineText,
                r.Kind
            })
        };
        Console.WriteLine(JsonSerializer.Serialize(result, BaseSettings.JsonOptions));
    }

    static void PrintFormatted(IReadOnlyList<SymbolReference> refs, Settings settings)
    {
        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(settings.Symbol)}[/] — {refs.Count} reference(s)  [grey][[syntactic]][/]\n");

        if (refs.Count == 0)
        {
            AnsiConsole.MarkupLine("  [yellow](no references found)[/]");
            return;
        }

        var table = new Table().Border(TableBorder.None);
        table.AddColumn(new TableColumn("[bold yellow]File[/]"));
        table.AddColumn(new TableColumn("[bold yellow]Line[/]"));
        table.AddColumn(new TableColumn("[bold yellow]Column[/]"));
        table.AddColumn(new TableColumn("[bold yellow]Source[/]"));
        table.AddColumn(new TableColumn("[bold yellow]Kind[/]"));

        foreach (var r in refs)
        {
            table.AddRow(
                $"[blue]{Markup.Escape(r.FilePath)}[/]",
                $"[bold]{r.Line}[/]",
                $"[bold]{r.Column}[/]",
                $"[dim]{Markup.Escape(r.LineText)}[/]",
                $"[cyan]{Markup.Escape(r.Kind)}[/]"
            );
        }

        AnsiConsole.Write(table);
    }
}
