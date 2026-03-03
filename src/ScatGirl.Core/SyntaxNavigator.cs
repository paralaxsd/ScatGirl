using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ScatGirl.Core;

public sealed class SyntaxNavigator
{
    public IReadOnlyList<SymbolDeclaration> FindDeclarations(
        string rootPath, string symbolName, string? kind = null)
    {
        var results = new List<SymbolDeclaration>();
        var absRoot = Path.GetFullPath(rootPath);

        foreach (var filePath in FileScanner.GetCSharpFiles(rootPath))
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));

            var walker = new DeclarationWalker(symbolName, kind);
            walker.Visit(tree.GetRoot());

            var relPath = Path.GetRelativePath(absRoot, filePath).Replace('\\', '/');
            foreach (var (node, decKind, containingType) in walker.Hits)
            {
                var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                results.Add(new(symbolName, decKind, containingType, new(relPath, line)));
            }
        }

        return results;
    }

    public IReadOnlyList<SymbolReference> FindReferences(
        string rootPath, string symbolName, string? kind = null, string? globFilter = null)
    {
        var results = new List<SymbolReference>();
        var absRoot = Path.GetFullPath(rootPath);

        foreach (var filePath in FileScanner.GetCSharpFiles(rootPath, globFilter))
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));

            var walker = new ReferenceWalker(symbolName, kind);
            walker.Visit(tree.GetRoot());

            var relPath = Path.GetRelativePath(absRoot, filePath).Replace('\\', '/');
            var lines   = tree.GetText().Lines;

            foreach (var (node, refKind) in walker.Hits)
            {
                var lineIndex = node.GetLocation().GetLineSpan().StartLinePosition.Line;
                var lineText  = lines[lineIndex].ToString().Trim();
                results.Add(new(relPath, lineIndex + 1, lineText, refKind));
            }
        }

        return results;
    }
}

sealed class DeclarationWalker(string symbolName, string? kind) : CSharpSyntaxWalker
{
    internal readonly List<(SyntaxNode Node, string Kind, string? ContainingType)> Hits = [];

    void TryAdd(SyntaxNode locationNode, string decKind, string name)
    {
        if (name != symbolName) return;
        if (kind is not null && !string.Equals(decKind, kind, StringComparison.OrdinalIgnoreCase)) return;

        Hits.Add((locationNode, decKind, GetContainingType(locationNode)));
    }

    static string? GetContainingType(SyntaxNode node)
    {
        var parent = node.Parent;
        while (parent is not null)
        {
            var name = parent switch
            {
                ClassDeclarationSyntax c     => c.Identifier.Text,
                InterfaceDeclarationSyntax i => i.Identifier.Text,
                RecordDeclarationSyntax r    => r.Identifier.Text,
                StructDeclarationSyntax s    => s.Identifier.Text,
                _                            => null
            };
            if (name is not null) return name;
            parent = parent.Parent;
        }
        return null;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        TryAdd(node, "class", node.Identifier.Text);
        base.VisitClassDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        TryAdd(node, "interface", node.Identifier.Text);
        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        TryAdd(node, "record", node.Identifier.Text);
        base.VisitRecordDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        TryAdd(node, "struct", node.Identifier.Text);
        base.VisitStructDeclaration(node);
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        TryAdd(node, "enum", node.Identifier.Text);
        base.VisitEnumDeclaration(node);
    }

    public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
    {
        TryAdd(node, "delegate", node.Identifier.Text);
        base.VisitDelegateDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        TryAdd(node, "method", node.Identifier.Text);
        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        TryAdd(node, "constructor", node.Identifier.Text);
        base.VisitConstructorDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        TryAdd(node, "property", node.Identifier.Text);
        base.VisitPropertyDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        foreach (var variable in node.Declaration.Variables)
            TryAdd(variable, "field", variable.Identifier.Text);
        base.VisitFieldDeclaration(node);
    }

    public override void VisitEventDeclaration(EventDeclarationSyntax node)
    {
        TryAdd(node, "event", node.Identifier.Text);
        base.VisitEventDeclaration(node);
    }

    public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
    {
        foreach (var variable in node.Declaration.Variables)
            TryAdd(variable, "event", variable.Identifier.Text);
        base.VisitEventFieldDeclaration(node);
    }
}

sealed class ReferenceWalker(string symbolName, string? kindFilter) : CSharpSyntaxWalker
{
    internal readonly List<(SyntaxNode Node, string Kind)> Hits = [];

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (node.Identifier.Text == symbolName)
        {
            var kind = ClassifyKind(node);
            if (kindFilter is null || string.Equals(kind, kindFilter, StringComparison.OrdinalIgnoreCase))
                Hits.Add((node, kind));
        }
        base.VisitIdentifierName(node);
    }

    static string ClassifyKind(IdentifierNameSyntax node) => node.Parent switch
    {
        TypeOfExpressionSyntax                          => "typeof",
        AttributeSyntax                                 => "attribute",
        QualifiedNameSyntax { Parent: AttributeSyntax } => "attribute",
        ArgumentSyntax arg when IsNameofArg(arg)        => "nameof",
        _                                               => "identifier"
    };

    static bool IsNameofArg(ArgumentSyntax arg) =>
        arg.Parent is ArgumentListSyntax
        {
            Parent: InvocationExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: "nameof" }
            }
        };
}
