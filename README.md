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

The solution uses the new XML-based `.slnx` format (Visual Studio 2022+):

```bash
dotnet restore
dotnet build
```

Or with the solution file explicitly:

```bash
dotnet build RtmLanguageServer.slnx
```

## Run Tests

```bash
dotnet test
```

## Run the Server

From the repo root:

```bash
dotnet run --project src/RtmLanguageServer.App/RtmLanguageServer.App.csproj
```

The server communicates over stdin/stdout using the Language Server Protocol, as standard LSP servers do.

## Installation & Quickstart

Prerequisites:

- .NET 10 SDK (or later)
- Java 17+ on PATH (required only for ANTLR code generation during build)
- A code editor that supports the Language Server Protocol (VS Code, Visual Studio, etc.)

Build and run locally:

```bash
dotnet restore
dotnet build
dotnet run --project src/RtmLanguageServer.App/RtmLanguageServer.App.csproj
```

Quickstart (VS Code)

1. Clone this repository and build the server (see commands above).
2. Install or run a client extension that launches the server for the `rtm` language (see recommended extension below).
3. Open an `.rtm` file and the client will start the language server; features such as Go to Definition, Find References and Hover will be available.

If you prefer to point a custom VS Code extension at the built executable, use the compiled executable or a dotnet run command in the extension's serverOptions.

<!-- Recommended language extension referenced below in Client integration section -->

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

## Client integration / RtmLanguageExtension

This repository contains the language server only. The recommended client is the companion project [RtmLanguageExtension](https://github.com/jamescfrost/RtmLanguageExtension), which is configured to look for the language server binary in the extension's `server` folder.

To integrate the built server with that extension, publish or copy the compiled output into the extension's server directory. Example (PowerShell, from repository root):

```powershell
# Build and publish framework-dependent output to the extension's server folder
dotnet publish src/RtmLanguageServer.App/RtmLanguageServer.App.csproj -c Release -o ../RtmLanguageExtension/server

# Alternatively, publish a self-contained executable (Windows x64 example)
dotnet publish src/RtmLanguageServer.App/RtmLanguageServer.App.csproj -c Release -r win-x64 --self-contained true -o ../RtmLanguageExtension/server
```

When the RtmLanguageExtension is launched (for example in VS Code Extension Development Host), it will look in its `server` folder and start the server executable found there. This makes it easy to iterate on the server implementation without changing the extension's code.

Note: The relative path above assumes both repositories are checked out side-by-side (../RtmLanguageExtension). Adjust the output path to match your local layout if different.

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

3. **RtmLanguageServer.App** - Executable host
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
dotnet run --project src/RtmLanguageServer.App/RtmLanguageServer.App.csproj
```

Contributing

Contributions are welcome. Please open issues for bugs or feature requests and submit pull requests against the master branch. When contributing, prefer small, focused changes and include unit tests for parser/semantic changes where possible.
