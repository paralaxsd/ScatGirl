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
        "Returns file paths relative to rootPath with 1-based line numbers. " +
        "C# only. Analyzes C# source files using Roslyn syntax analysis. " +
        "Do not use for Python, TypeScript, or other languages.")]
    static string FindDeclarations(
        [Description("Absolute path to the repository root")] string rootPath,
        [Description("Symbol name to find (e.g. \"IUserService\", \"ProcessPayment\")")] string symbolName,
        [Description("Optional kind filter: class, interface, method, property, record, struct, " +
                     "enum, field, constructor, delegate, event")] string? kind = null)
    {
        IReadOnlyList<SymbolDeclaration> declarations;
        try { declarations = new SyntaxNavigator().FindDeclarations(rootPath, symbolName, kind); }
        catch (Exception ex) { return Error(ex.Message); }

        var declResults = declarations.Select(d => new
        {
            d.Name,
            d.Kind,
            d.ContainingType,
            filePath = d.Location.FilePath,
            line     = d.Location.Line
        });

        if (declarations.Count == 0)
            return Serialize(new
            {
                root = rootPath,
                symbolName,
                kind,
                count = 0,
                hint  = "No declarations found in local source. Symbol may be defined in an external assembly — try ScatMan.",
                declarations = declResults
            });

        return Serialize(new
        {
            root = rootPath,
            symbolName,
            kind,
            count        = declarations.Count,
            declarations = declResults
        });
    }

    [McpServerTool(Name = "find_members")]
    [Description(
        "List all members of a named type in a C# codebase. " +
        "Works on raw source files without requiring compilation or dotnet restore. " +
        "Returns fields, properties, constructors, methods, and events with signatures and line numbers. " +
        "Only shows members declared directly in the type (not inherited members). " +
        "C# only. Analyzes C# source files using Roslyn syntax analysis. " +
        "Do not use for Python, TypeScript, or other languages.")]
    static string FindMembers(
        [Description("Absolute path to the repository root")] string rootPath,
        [Description("Type name to inspect (e.g. \"AudioCaptureService\", \"SyntaxNavigator\")")] string typeName,
        [Description("Optional member kind filter: method, property, field, constructor, event")] string? kind = null,
        [Description("Optional glob pattern to restrict search (e.g. \"**/*Service.cs\")")] string? inFile = null)
    {
        IReadOnlyList<TypeMember> members;
        try { members = new SyntaxNavigator().FindMembers(rootPath, typeName, kind, inFile); }
        catch (Exception ex) { return Error(ex.Message); }

        return Serialize(new
        {
            root     = rootPath,
            typeName,
            kind,
            inFile,
            count    = members.Count,
            members  = members.Select(m => new
            {
                m.Kind,
                m.Signature,
                filePath = m.Location.FilePath,
                line     = m.Location.Line
            })
        });
    }

    [McpServerTool(Name = "find_references")]
    [Description(
        "Find all references to a named symbol in a C# codebase (syntactic). " +
        "Works on raw source files without requiring compilation or dotnet restore. " +
        "Returns file paths, 1-based line numbers, and the matching source line for each hit. " +
        "C# only. Analyzes C# source files using Roslyn syntax analysis. " +
        "Do not use for Python, TypeScript, or other languages.")]
    static string FindReferences(
        [Description("Absolute path to the repository root")] string rootPath,
        [Description("Symbol name to find references to (e.g. \"AudioCaptureService\", \"IUserService\")")] string symbolName,
        [Description("Optional kind filter: identifier, typeof, nameof, attribute, " +
                     "implementation, invocation, object-creation, type-argument")] string? kind = null,
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
                r.Column,
                r.LineText,
                r.Kind
            })
        });
    }

    static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    static string Error(string message) =>
        JsonSerializer.Serialize(new { error = message }, JsonOptions);
}
