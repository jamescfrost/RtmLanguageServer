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

- .NET 8 SDK
- Java available on `PATH` for ANTLR generation at build time

NuGet packages used:

- `Antlr4.Runtime.Standard`
- `Antlr4BuildTasks`

## Build

```bash
dotnet restore
dotnet build
```

From the repo root:

```bash
dotnet run --project src/RtmLanguageServer/RtmLanguageServer.csproj
```

The server communicates over stdin/stdout, as normal LSP servers do.

## Grammar

The uploaded grammar files are included here:

```text
src/RtmLanguageServer/Grammar/RtmLexer.g4
src/RtmLanguageServer/Grammar/RtmParser.g4
```

`Antlr4BuildTasks` generates the C# lexer/parser during build.

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

## VS Code client sketch

Create a small VS Code extension and point the client command at the built executable:

```ts
import * as vscode from 'vscode';
import * as lc from 'vscode-languageclient/node';

let client: lc.LanguageClient;

export function activate(context: vscode.ExtensionContext) {
  const serverExe = '/absolute/path/to/RtmLanguageServer';

  client = new lc.LanguageClient(
    'rtmLanguageServer',
    'RTM Language Server',
    { command: serverExe },
    {
      documentSelector: [{ scheme: 'file', language: 'rtm' }],
      synchronize: { fileEvents: vscode.workspace.createFileSystemWatcher('**/*.{rtm,zug}') }
    }
  );

  context.subscriptions.push(client.start());
}

export function deactivate() {
  return client?.stop();
}
```

For development, you can also set the command to:

```text
dotnet
```

with args:

```json
["run", "--project", "/absolute/path/to/src/RtmLanguageServer/RtmLanguageServer.csproj"]
```

## Next improvements

1. Add workspace indexing for all `.rtm` / `.zug` files, not just opened files.
2. Add scopes: overlay scope, proc scope, parameter scope, data-area scope.
3. Publish diagnostics from `RtmDocument.Diagnostics`.
4. Add completion from known definitions.
5. Add semantic tokens for fields, procs, overlays, literals, and comments.
