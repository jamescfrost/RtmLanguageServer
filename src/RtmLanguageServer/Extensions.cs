using RtmLanguageServer.Analysis;
using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer
{
    public static class Extensions
    {
        public static string? ResolveSymbolName(this RtmDocument document, TextPosition position)
        {
            // Prefer the indexed occurrence under the cursor. This means definition,
            // reference and hover work from either a declaration or a usage.
            var definition = document.DefinitionAt(position);
            if (definition is not null) return definition.Name;

            var reference = document.ReferenceAt(position);
            if (reference is not null) return reference.Name;

            return document.SymbolNameAt(position);
        }
    }
}