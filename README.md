# ScatGirl [![ci](https://github.com/paralaxsd/ScatGirl/actions/workflows/ci.yml/badge.svg)](https://github.com/paralaxsd/ScatGirl/actions/workflows/ci.yml) [![CLI NuGet](https://img.shields.io/nuget/v/ScatGirl.Cli.svg?color=blue)](https://www.nuget.org/packages/ScatGirl.Cli) [![MCP NuGet](https://img.shields.io/nuget/v/ScatGirl.Mcp.svg?color=blue)](https://www.nuget.org/packages/ScatGirl.Mcp)
![ScatGirl logo](https://raw.githubusercontent.com/paralaxsd/ScatGirl/main/images/logo.png)

> *Improvising over unfamiliar codebases since 2026.*

A good jazz musician doesn't need the score to know where the melody goes — they hear a phrase and follow it anywhere. ScatGirl does the same with source code: drop it a symbol name, and it riffs back with every definition, every caller, every implementation.

**What it is:** An MCP server for symbolic C# source code navigation. Point it at a repository, ask it where `IUserService` is implemented or who calls `ProcessPayment` — and get semantic answers, not text matches.

**How it works:** Roslyn's syntax APIs parse raw `.cs` files without requiring compilation or `dotnet restore`. Fast, always available, degrades gracefully.

Sister project to [ScatMan](https://github.com/paralaxsd/ScatMan): ScatMan answers *"what can I call on this NuGet package?"* — ScatGirl answers *"how is it actually used here?"* Together they cover the full loop from external API discovery to local implementation exploration.

## Tools

### `find_declarations`
Find all declarations of a named symbol across a C# codebase.

```json
{
  "rootPath": "C:/projects/MyApp",
  "symbolName": "IUserService",
  "kind": "interface"
}
```

```json
{
  "root": "C:/projects/MyApp",
  "symbolName": "IUserService",
  "kind": "interface",
  "count": 1,
  "declarations": [
    { "name": "IUserService", "kind": "interface", "containingType": null,
      "filePath": "src/Core/IUserService.cs", "line": 5 }
  ]
}
```

**kind** filter (optional): `class` `interface` `record` `struct` `enum` `delegate` `method` `constructor` `property` `field` `event`

### `find_references`
Find all references to a named symbol across a C# codebase. Returns file, line, and the matching source line for each hit. Results are tagged `[syntactic]` — no compilation required, name-based matching.

```json
{
  "rootPath": "C:/projects/MyApp",
  "symbolName": "AudioCaptureService",
  "kind": "identifier",
  "inFile": "**/*Service.cs"
}
```

```json
{
  "root": "C:/projects/MyApp",
  "symbolName": "AudioCaptureService",
  "analysis": "syntactic",
  "count": 2,
  "references": [
    { "filePath": "src/Program.cs", "line": 38,
      "lineText": "builder.Services.AddHostedService<AudioCaptureService>();", "kind": "identifier" },
    { "filePath": "src/AudioCaptureService.cs", "line": 12,
      "lineText": "sealed class AudioCaptureService : IHostedService", "kind": "identifier" }
  ]
}
```

**kind** filter (optional): `identifier` `typeof` `nameof` `attribute` `implementation` `invocation` `object-creation` `type-argument`
**inFile** (optional): glob pattern, e.g. `**/*Service.cs`

### `find_members`
List all members declared directly in a named type — fields, properties, constructors, methods, and events. Only own members are shown (no inherited members).

```json
{
  "rootPath": "C:/projects/MyApp",
  "typeName": "AudioCaptureService",
  "kind": "method"
}
```

```json
{
  "root": "C:/projects/MyApp",
  "typeName": "AudioCaptureService",
  "count": 2,
  "members": [
    { "kind": "method", "signature": "public Task StartAsync(CancellationToken ct)",
      "filePath": "src/AudioCaptureService.cs", "line": 45 },
    { "kind": "method", "signature": "public Task StopAsync(CancellationToken ct)",
      "filePath": "src/AudioCaptureService.cs", "line": 67 }
  ]
}
```

**kind** filter (optional): `field` `property` `constructor` `method` `event`
**inFile** (optional): glob pattern — useful when the type name is ambiguous across namespaces

## MCP Server

ScatGirl ships as an **MCP stdio server** — use it directly from Claude Code, Claude Desktop, or any MCP-compatible client without ever opening a terminal.

```bash
dotnet tool install --global ScatGirl.Mcp
```

### Claude Code

```bash
claude mcp add ScatGirl --scope user -- scatgirl-mcp
```

> `--scope user` is required so the server is available globally, not just within one project directory.

### Claude Desktop

`~/.claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "ScatGirl": {
      "command": "scatgirl-mcp"
    }
  }
}
```

### Available tools

| Tool | Description |
|---|---|
| `find_declarations` | Find all declarations of a named symbol (`rootPath`, `symbolName`, `kind?`, `regex?`) |
| `find_references` | Find all references to a named symbol (`rootPath`, `symbolName`, `kind?`, `inFile?`, `regex?`) |
| `find_members` | List all members of a named type (`rootPath`, `typeName`, `kind?`, `inFile?`) |
| `meta` | Show build and runtime metadata for ScatGirl MCP/CLI |

---

## CLI

```bash
dotnet tool install --global ScatGirl.Cli
scatgirl find . IUserService
scatgirl find . ProcessPayment --kind method

scatgirl refs . AudioCaptureService
scatgirl refs . IMonitoringStateService --kind implementation
scatgirl refs . IMonitoringStateService --kind identifier
scatgirl refs . NoiseDetector --in-file "**/*Service.cs"
scatgirl refs . AppJsonSerializerContext --json

scatgirl members . AudioCaptureService
scatgirl members . AudioCaptureService --kind method
scatgirl members . SyntaxNavigator --in-file "**/ScatGirl.Core/**"
scatgirl members . SyntaxNavigator --json
```
