namespace PartyGame.TypeGen;

/// <summary>
/// Compares a generated text with a file on disk, and brings the file up to date.
/// </summary>
internal static class OutputFile
{
    /// <summary>
    /// Whether the file exists with exactly this content, whatever its line endings.
    /// </summary>
    public static bool IsUpToDate(string path, string content) =>
        File.Exists(path) && NormalizeLineEndings(File.ReadAllText(path)) == content;

    /// <summary>
    /// Describes how the file differs from the content, or returns <see langword="null"/>.
    /// </summary>
    /// <param name="path">The file on disk.</param>
    /// <param name="content">The generated content, with LF line endings.</param>
    /// <param name="name">The name of the file in the description.</param>
    public static string? FindDifference(string path, string content, string name) =>
        !File.Exists(path) ? $"{name} is missing"
        : !IsUpToDate(path, content) ? $"{name} is out of date"
        : null;

    /// <summary>
    /// Writes the content unless the file is already up to date, creating its directory if needed.
    /// </summary>
    public static void Write(string path, string content)
    {
        if (IsUpToDate(path, content))
        {
            return;
        }

        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } directory)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, content);
    }

    // Git may check files out with CRLF on Windows; only the content matters.
    private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
