namespace RtmLanguageServer.Core.Common;

public sealed record RtmDiagnostic(
    TextRange Range,
    DiagnosticSeverity Severity,
    string Source,
    string Message);
