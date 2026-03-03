namespace ScatGirl.Core;

public record Location(string FilePath, int Line);

public record SymbolDeclaration(string Name, string Kind, string? ContainingType, Location Location);

public record SymbolReference(string FilePath, int Line, string LineText, string Kind);

public record TypeMember(string Kind, string Signature, Location Location);
