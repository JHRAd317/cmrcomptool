namespace CmrCompTool.WebGui.Services;

public class DownloadsService
{
    private readonly string _downloadsPath;

    public DownloadsService()
    {
        // Resolve the current user's Downloads folder.
        // On Windows, UserProfile + Downloads is the standard location.
        // Using SHGetKnownFolderPath for correctness would require P/Invoke;
        // Environment.SpecialFolder.UserProfile + "Downloads" is reliable in practice.
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _downloadsPath = Path.Combine(userProfile, "Downloads");
    }

    /// <summary>Returns the resolved absolute path to the current user's Downloads folder.</summary>
    public string DownloadsPath => _downloadsPath;

    /// <summary>
    /// Resolves a caller-supplied value (bare filename or full path) to a fully-qualified
    /// path that is guaranteed to be inside Downloads.  Throws if the resolved path escapes.
    /// </summary>
    public string ResolveSafe(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Path must not be empty.", nameof(value));

        // If the caller supplied a bare filename (no directory separators), combine with Downloads.
        string candidate = Path.IsPathRooted(value)
            ? Path.GetFullPath(value)
            : Path.GetFullPath(Path.Combine(_downloadsPath, value));

        // Normalise both paths for comparison (trailing separator on downloads).
        var downloadsNorm = _downloadsPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(downloadsNorm, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(
                $"Access denied: path must be inside Downloads ({_downloadsPath}).");

        return candidate;
    }
}
