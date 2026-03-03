using System.ComponentModel;
using System.Text.Json;
using ScatGirl.Cli;
using ScatGirl.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatGirl.Cli.Commands;

sealed class FindDeclarationsCommand : Command<FindDeclarationsCommand.Settings>
{
    public sealed class Settings : BaseSettings
    {
        [CommandArgument(1, "<symbol>")]
        [Description("Symbol name to find (e.g. IUserService, ProcessPayment)")]
        public string Symbol { get; init; } = "";

        [CommandOption("-k|--kind")]
        [Description("Filter by kind: class, interface, method, property, record, struct, enum, " +
                     "field, constructor, delegate, event")]
        public string? Kind { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken ct)
    {
        var declarations = new SyntaxNavigator().FindDeclarations(settings.Root, settings.Symbol, settings.Kind);

        if (settings.Json) PrintJson(declarations, settings);
        else PrintFormatted(declarations, settings);

        return 0;
    }

    static void PrintJson(IReadOnlyList<SymbolDeclaration> declarations, Settings settings)
    {
        var result = new
        {
            root = settings.Root,
            symbolName = settings.Symbol,
            kind = settings.Kind,
            count = declarations.Count,
            declarations = declarations.Select(d => new
            {
                d.Name,
                d.Kind,
                d.ContainingType,
                filePath = d.Location.FilePath,
                line = d.Location.Line
            })
        };
        Console.WriteLine(JsonSerializer.Serialize(result, BaseSettings.JsonOptions));
    }

    static void PrintFormatted(IReadOnlyList<SymbolDeclaration> declarations, Settings settings)
    {
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(settings.Symbol)}[/] — {declarations.Count} declaration(s)\n");

        if (declarations.Count == 0)
        {
            AnsiConsole.MarkupLine("  [yellow](no declarations found)[/]");
            return;
        }

        var table = new Table().BorderStyle(Style.Plain).HideHeaders();
        table.AddColumn("kind");
        table.AddColumn("location");
        table.AddColumn("container");

        foreach (var d in declarations)
        {
            var container = d.ContainingType is not null
                ? $"[grey]in {Markup.Escape(d.ContainingType)}[/]"
                : "";

            table.AddRow(
                $"[cyan]{Markup.Escape(d.Kind)}[/]",
                $"{Markup.Escape(d.Location.FilePath)}:[bold]{d.Location.Line}[/]",
                container);
        }

        AnsiConsole.Write(table);
    }
}
