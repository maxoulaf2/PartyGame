using System.Globalization;

namespace PartyGame.Content;

/// <summary>
/// Builds the JSON paths that locate a problem in a descriptor, such as <c>$.rounds[1].questions[4].choices</c>.
/// </summary>
internal static class JsonPath
{
    public const string Root = "$";

    public static string Property(string path, string name) =>
        IsIdentifier(name)
            ? $"{path}.{name}"
            : $"{path}['{name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal)}']";

    public static string Index(string path, int index) =>
        string.Create(CultureInfo.InvariantCulture, $"{path}[{index}]");

    // The dotted form is the one authors read best; any other name (a misspelled property may hold anything) is quoted.
    private static bool IsIdentifier(string name) =>
        name.Length > 0
        && !char.IsAsciiDigit(name[0])
        && name.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '$');
}
