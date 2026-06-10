namespace RtmLanguageServer.Core.Common;

public sealed class LocationEqualityComparer : IEqualityComparer<Location>, IComparer<Location>
{
    public static LocationEqualityComparer Instance { get; } = new();

    private LocationEqualityComparer() { }

    public bool Equals(Location? x, Location? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return string.Equals(x.Uri, y.Uri, StringComparison.OrdinalIgnoreCase)
            && x.Range.Equals(y.Range);
    }

    public int GetHashCode(Location obj)
    {
        return HashCode.Combine(
            obj.Uri.ToLowerInvariant(),
            obj.Range);
    }

    public int Compare(Location? x, Location? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var uriCompare = string.Compare(x.Uri, y.Uri, StringComparison.OrdinalIgnoreCase);
        if (uriCompare != 0) return uriCompare;

        var lineCompare = x.Range.Start.Line.CompareTo(y.Range.Start.Line);
        if (lineCompare != 0) return lineCompare;

        return x.Range.Start.Character.CompareTo(y.Range.Start.Character);
    }
}
