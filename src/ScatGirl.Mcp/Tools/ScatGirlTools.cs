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

    [McpServerTool(Name = "find_references")]
    [Description(
        "Find all references to a named symbol in a C# codebase (syntactic). " +
        "Works on raw source files without requiring compilation or dotnet restore. " +
        "Returns file paths, 1-based line numbers, and the matching source line for each hit.")]
    static string FindReferences(
        [Description("Absolute path to the repository root")] string rootPath,
        [Description("Symbol name to find references to (e.g. \"AudioCaptureService\", \"IUserService\")")] string symbolName,
        [Description("Optional kind filter: identifier, typeof, nameof, attribute")] string? kind = null,
        [Description("Optional glob pattern to restrict search (e.g. \"**/*Service.cs\")")] string? inFile = null)
    {
        IReadOnlyList<SymbolReference> refs;
        try { refs = new SyntaxNavigator().FindReferences(rootPath, symbolName, kind, inFile); }
        catch (Exception ex) { return Error(ex.Message); }

        return Serialize(new
        {
            root       = rootPath,
            symbolName,
            kind,
            inFile,
            analysis   = "syntactic",
            count      = refs.Count,
            references = refs.Select(r => new
            {
                r.FilePath,
                r.Line,
                r.LineText,
                r.Kind
            })
        });
    }

    static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    static string Error(string message) =>
        JsonSerializer.Serialize(new { error = message }, JsonOptions);
}
