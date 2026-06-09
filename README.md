# RTM Language Server

A minimal C# Language Server Protocol implementation for the supplied `RtmLexer.g4` and `RtmParser.g4` grammars.

It supports:

- `textDocument/definition`
- `textDocument/references`
- `textDocument/hover`
- Full-document sync through `didOpen` / `didChange`
- ANTLR parsing on every document update

The server intentionally uses a tiny raw JSON-RPC/LSP transport instead of an LSP framework so the first iteration is easy to inspect and modify.

## Requirements

- .NET 10.0 SDK (or later)
- Java 17+ available on `PATH` for ANTLR code generation at build time

NuGet packages used:

- `Antlr4.Runtime.Standard` (4.13.1)
- `Antlr4BuildTasks` (12.14.0)

## Build

```bash
dotnet restore
dotnet build
```

## Run Tests

```bash
dotnet test
```

## Run the Server

From the repo root:

```bash
dotnet run --project src/RtmLanguageServer/RtmLanguageServer.csproj
```

The server communicates over stdin/stdout using the Language Server Protocol, as standard LSP servers do.

## Grammar

The grammar files are located in the Core library:

```text
src/RtmLanguageServer.Core/Grammar/RtmLexer.g4
src/RtmLanguageServer.Core/Grammar/RtmParser.g4
```

`Antlr4BuildTasks` generates the C# lexer/parser during build into the `RtmLanguageServer.Core.Generated` namespace.

## Current symbol model

Definitions are collected from:

- `$ENTRY ID` as `Overlay`
- `ID PROC ... ENDPROC` as `Procedure`
- `$DATA` fields as `Field`
- `$PROG(...)`, `PROC(...)`, and `$ABORT(...)` parameter lists as `Parameter`
- `$FILES` IDs as `File`
- `$EXT` IDs as `External`

References are collected from:

- `identifier_expression`
- `assignable`
- `function_call`
- `copy_mask`

This is deliberately broad. The next useful refinement is adding lexical/scope rules so local proc parameters do not collide with fields or other procedures.

## VS Code Client Setup

Create a small VS Code extension and point the client command at the built executable:

**TypeScript Example:**

```ts
import * as vscode from 'vscode';
import * as lc from 'vscode-languageclient/node';

let client: lc.LanguageClient;

export function activate(context: vscode.ExtensionContext) {
  const serverOptions: lc.ServerOptions = {
    command: 'dotnet',
    args: ['run', '--project', '/absolute/path/to/src/RtmLanguageServer/RtmLanguageServer.csproj']
  };

  const clientOptions: lc.LanguageClientOptions = {
    documentSelector: [{ scheme: 'file', language: 'rtm' }],
    synchronize: {
      fileEvents: vscode.workspace.createFileSystemWatcher('**/*.{rtm,zug}')
    }
  };

  client = new lc.LanguageClient(
    'rtmLanguageServer',
    'RTM Language Server',
    serverOptions,
    clientOptions
  );

  context.subscriptions.push(client.start());
}

export function deactivate() {
  return client?.stop();
}
```

**Alternative: Use the compiled executable:**

```ts
const serverOptions: lc.ServerOptions = {
  command: '/absolute/path/to/RtmLanguageServer.exe' // or .dll on non-Windows
};
```

## Code Style

This project uses `.editorconfig` to enforce consistent code style. Visual Studio 2026 and VS Code (with the EditorConfig extension) will automatically apply these rules. The configuration enforces:

- 4-space indentation for C# files
- File-scoped namespaces
- Modern C# conventions
- Consistent naming conventions

## Architecture

The solution is organized into three logical layers:

### Projects

1. **RtmLanguageServer.Core** - Reusable RTM language analysis library
   - Grammar definitions (ANTLR4 lexer and parser)
   - Symbol model (overlays, procedures, fields, references, etc.)
   - Parsing and semantic analysis
   - Preprocessor (`$NAME` sections and `$INCLUDE` directives)
   - No dependencies on LSP or JSON-RPC

2. **RtmLanguageServer.Protocol** - LSP protocol implementation
   - LSP models and DTOs (initialize, textDocument, hover, diagnostics, etc.)
   - JSON-RPC message transport (stdin/stdout)
   - Document store and workspace management
   - Language service (definition, references, hover implementation)
   - Depends on: Core

3. **RtmLanguageServer** - Executable host
   - Entry point (`Program.cs`)
   - Logging configuration
   - Server bootstrapping
   - Depends on: Protocol

4. **RtmLanguageServer.Tests** - Unit tests
   - Parse tests
   - Semantic resolution tests
   - Diagnostic tests
   - Depends on: Core

### Benefits

- **Separation of concerns**: Grammar/analysis logic is independent of LSP protocol
- **Reusability**: Core library can be used in CLI tools, formatters, or other analyzers
- **Testability**: Each layer can be tested independently
- **Maintainability**: Clear boundaries and namespaces matching folder structure

### LSP Features Implemented

- ✅ `textDocument/definition` - Go to definition
- ✅ `textDocument/references` - Find all references
- ✅ `textDocument/hover` - Symbol information on hover
- ✅ `textDocument/publishDiagnostics` - Syntax error reporting
- ✅ Full-document sync (`didOpen`, `didChange`)
- ✅ Workspace indexing for multi-file projects
- ✅ Cross-file symbol resolution
- ✅ `$INCLUDE` preprocessing with source mapping

### Planned Features

See the [Issues](../../issues) page for planned enhancements including:
- Code completion
- Semantic tokens (syntax highlighting)
- Document symbols (outline view)
- Signature help

## Development

For development, you can use the following `dotnet run` command with arguments:

```bash
dotnet run --project src/RtmLanguageServer/RtmLanguageServer.csproj
```
