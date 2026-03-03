using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using ScatGirl.Core;

namespace ScatGirl.Mcp.Tools;

[McpServerToolType]
static class ScatGirlTools
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented    = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [McpServerTool(Name = "find_declarations")]
    [Description(
        "Find all declarations of a named symbol in a C# codebase. " +
        "Works on raw source files without requiring compilation or dotnet restore. " +
        "Returns file paths relative to rootPath with 1-based line numbers.")]
    static string FindDeclarations(
        [Description("Absolute path to the repository root")] string rootPath,
        [Description("Symbol name to find (e.g. \"IUserService\", \"ProcessPayment\")")] string symbolName,
        [Description("Optional kind filter: class, interface, method, property, record, struct, " +
                     "enum, field, constructor, delegate, event")] string? kind = null)
    {
        IReadOnlyList<SymbolDeclaration> declarations;
        try { declarations = new SyntaxNavigator().FindDeclarations(rootPath, symbolName, kind); }
        catch (Exception ex) { return Error(ex.Message); }

        return Serialize(new
        {
            root = rootPath,
            symbolName,
            kind,
            count = declarations.Count,
            declarations = declarations.Select(d => new
            {
                d.Name,
                d.Kind,
                d.ContainingType,
                filePath = d.Location.FilePath,
                line     = d.Location.Line
            })
        });
    }

    static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    static string Error(string message) =>
        JsonSerializer.Serialize(new { error = message }, JsonOptions);
}
