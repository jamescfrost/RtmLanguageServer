using RtmLanguageServer.Analysis.Symbols;

namespace RtmLanguageServer.Analysis
{
    public static class SemanticResolver
    {
        public static void Resolve(RtmDocument document) => Resolve(document, [document]);

        public static void ResolveAll(IEnumerable<RtmDocument> documents)
        {
            var snapshot = documents.ToArray();
            foreach (var document in snapshot)
                Resolve(document, snapshot);
        }

        public static void Resolve(RtmDocument document, IEnumerable<RtmDocument> documents)
        {
            var snapshot = documents as IReadOnlyCollection<RtmDocument> ?? documents.ToArray();

            ResolveIncludes(document, snapshot);

            foreach (var reference in document.Index.AllReferences)
            {
                if (reference.ResolvedSymbol != null)
                    continue;

                var candidates = snapshot
                    .SelectMany(d => d.Index.AllDefinitions)
                    .Where(s => string.Equals(s.Name, reference.Name, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(s => SymbolTypeScore(reference, s))
                    .ThenByDescending(s => ReferenceAndSymbolShareOverlay(reference, s))
                    .ThenByDescending(s => string.Equals(s.Location.Uri, reference.Location.Uri, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(s => s.Location.Range.Start.Line)
                    .ThenBy(s => s.Location.Range.Start.Character)
                    .ToList();

                reference.ResolvedSymbol = candidates.FirstOrDefault();
            }
        }

        private static void ResolveIncludes(RtmDocument document, IReadOnlyCollection<RtmDocument> documents)
        {
            var currentSourceName = RtmUri.SourceNameFromUri(document.Uri);

            foreach (var include in document.Index.AllReferences.OfType<IncludeReference>())
            {
                var targetSourceName = include.SourceName == "*"
                    ? currentSourceName
                    : include.SourceName;

                include.ResolvedSymbol = documents
                    .SelectMany(d => d.Index.AllDefinitions.OfType<NameIncludeSymbol>())
                    .Where(s => string.Equals(s.Name, include.IncludeName, StringComparison.OrdinalIgnoreCase))
                    .Where(s => string.Equals(RtmUri.SourceNameFromUri(s.Location.Uri), targetSourceName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s.Location.Range.Start.Line)
                    .ThenBy(s => s.Location.Range.Start.Character)
                    .FirstOrDefault();
            }
        }

        private static int SymbolTypeScore(RtmReference reference, RtmSymbol symbol) => reference switch
        {
            OverlayReference when symbol is OverlaySymbol => 100,
            OverlayReference when symbol is ExternalSymbol => -100,
            ProcedureReference when symbol is ProcedureSymbol => 100,
            ProcedureReference when symbol is ExternalSymbol => 10,
            FieldReference when symbol is FieldSymbol => 100,
            IncludeReference when symbol is NameIncludeSymbol => 100,
            _ => 0
        };

        private static bool ReferenceAndSymbolShareOverlay(RtmReference reference, RtmSymbol symbol)
        {
            if (reference.Overlay is null || symbol.Overlay is null)
                return false;

            return string.Equals(reference.Overlay.Name, symbol.Overlay.Name, StringComparison.OrdinalIgnoreCase);
        }

    }
}
