namespace ScatGirl.Core;

public record Location(string FilePath, int Line);

public record SymbolDeclaration(string Name, string Kind, string? ContainingType, Location Location);
