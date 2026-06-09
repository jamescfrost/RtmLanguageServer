namespace RtmLanguageServer.Core.Common;

public sealed class LocationEqualityComparer : IEqualityComparer<Location>
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
}
