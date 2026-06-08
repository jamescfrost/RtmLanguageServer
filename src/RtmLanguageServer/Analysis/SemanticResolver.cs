using RtmLanguageServer.Analysis.Symbols;

namespace RtmLanguageServer.Analysis
{
    public static class SemanticResolver
    {
        public static void Resolve(RtmDocument document)
        {
            ResolveIncludes(document);
            foreach (var reference in document.Index.AllReferences)
            {
                if (reference.ResolvedSymbol != null)
                    continue;
                var candidates = document.Index.AllDefinitions
                    .Where(s =>
                        s.Overlay == reference.Overlay
                        && string.Equals(s.Name, reference.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                reference.ResolvedSymbol = candidates.FirstOrDefault();
            }
        }

        private static void ResolveIncludes(RtmDocument document)
        {
            var nameIncludes = document.Index.AllDefinitions
                .OfType<NameIncludeSymbol>()
                .ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var include in document.Index.AllReferences.OfType<IncludeReference>())
            {
                if (include.SourceName != "*")
                    continue;

                if (nameIncludes.TryGetValue(include.IncludeName, out var target))
                    include.ResolvedSymbol = target;
            }
        }
    }
}