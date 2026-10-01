namespace PartyGame.TypeGen;

/// <summary>
/// Compares generated files with an output directory, and brings the directory up to date.
/// The directory belongs to the generator: any other <c>.ts</c> file in it is stale and removed.
/// </summary>
internal static class OutputDirectory
{
    private const string Pattern = "*.ts";

    /// <summary>
    /// Describes each difference between the directory and the generated files, or returns an empty list.
    /// </summary>
    public static IReadOnlyList<string> FindDifferences(string directory, IReadOnlyCollection<GeneratedFile> files)
    {
        var differences = new List<string>();

        foreach (var file in files.OrderBy(f => f.Path, StringComparer.Ordinal))
        {
            var path = Path.Combine(directory, file.Path);
            if (!File.Exists(path))
            {
                differences.Add($"{file.Path} is missing");
            }
            else if (NormalizeLineEndings(File.ReadAllText(path)) != file.Content)
            {
                differences.Add($"{file.Path} is out of date");
            }
        }

        differences.AddRange(StaleFiles(directory, files).Select(name => $"{name} is no longer generated"));
        return differences;
    }

    /// <summary>
    /// Writes the generated files that changed and deletes stale ones.
    /// </summary>
    public static void Write(string directory, IReadOnlyCollection<GeneratedFile> files)
    {
        Directory.CreateDirectory(directory);

        foreach (var name in StaleFiles(directory, files))
        {
            File.Delete(Path.Combine(directory, name));
        }

        foreach (var file in files)
        {
            var path = Path.Combine(directory, file.Path);
            if (!File.Exists(path) || NormalizeLineEndings(File.ReadAllText(path)) != file.Content)
            {
                File.WriteAllText(path, file.Content);
            }
        }
    }

    private static List<string> StaleFiles(string directory, IReadOnlyCollection<GeneratedFile> files)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var expected = files.Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Directory.EnumerateFiles(directory, Pattern)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !expected.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    // Git may check files out with CRLF on Windows; only the content matters.
    private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
