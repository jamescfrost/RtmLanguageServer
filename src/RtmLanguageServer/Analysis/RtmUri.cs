namespace RtmLanguageServer.Analysis;

public static class RtmUri
{
    public static string Normalize(string uriOrPath)
    {
        if (string.IsNullOrWhiteSpace(uriOrPath))
            return uriOrPath;

        uriOrPath = uriOrPath.Trim();

        if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out var parsed) && parsed.IsFile)
            return LocalPathToUri(parsed.LocalPath);

        if (LooksLikeWindowsAbsolutePath(uriOrPath) || Path.IsPathRooted(uriOrPath))
            return LocalPathToUri(uriOrPath);

        return uriOrPath;
    }

    public static string? ToLocalPath(string? uriOrPath)
    {
        if (string.IsNullOrWhiteSpace(uriOrPath))
            return null;

        uriOrPath = uriOrPath.Trim();

        if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out var parsed) && parsed.IsFile)
            return parsed.LocalPath;

        if (LooksLikeWindowsAbsolutePath(uriOrPath) || Path.IsPathRooted(uriOrPath))
            return uriOrPath;

        return null;
    }

    public static string SourceNameFromUri(string uriOrPath)
    {
        var localPath = ToLocalPath(uriOrPath) ?? uriOrPath;
        return Path.GetFileNameWithoutExtension(localPath).ToUpperInvariant();
    }

    public static string DisplayName(string uriOrPath)
    {
        var localPath = ToLocalPath(uriOrPath) ?? uriOrPath;
        return Path.GetFileName(localPath);
    }

    private static string LocalPathToUri(string path)
    {
        // Uri.LocalPath can return a leading slash for Windows file URIs when running under
        // non-Windows runtimes. Strip only the artificial slash from /D:/... paths.
        if (path.Length >= 3 && path[0] == '/' && char.IsLetter(path[1]) && path[2] == ':')
            path = path[1..];

        if (LooksLikeWindowsAbsolutePath(path))
        {
            var normalized = path.Replace('\\', '/');
            return "file:///" + Uri.EscapeDataString(normalized);
        }

        return new Uri(Path.GetFullPath(path)).AbsoluteUri;
    }

    private static bool LooksLikeWindowsAbsolutePath(string value) =>
        value.Length >= 3
        && char.IsLetter(value[0])
        && value[1] == ':'
        && (value[2] == '\\' || value[2] == '/');
}
