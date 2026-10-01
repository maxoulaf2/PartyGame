namespace PartyGame.TypeGen;

/// <summary>
/// A file to write in the output directory.
/// </summary>
/// <param name="Path">Path relative to the output directory, with forward slashes.</param>
/// <param name="Content">Full content, with LF line endings.</param>
internal sealed record GeneratedFile(string Path, string Content);
