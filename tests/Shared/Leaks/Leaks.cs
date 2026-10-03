using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PartyGame.Tests.Shared.Leaks;

/// <summary>
/// Looks for leaks in the JSON of a projection. The search walks the decoded JSON rather than its text: the serializer
/// escapes non-ASCII characters, so that a text search for <c>Échauffement</c> would never find <c>\u00C9chauffement</c>.
/// </summary>
internal static class Leaks
{
    // Only to show values in the failure messages as a human writes them.
    private static readonly JsonSerializerOptions _readable = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private const int MaxShownLength = 80;

    /// <summary>
    /// Where the secrets hidden from <paramref name="viewer"/> appear in its JSON, one finding per occurrence.
    /// </summary>
    public static IEnumerable<string> SecretsShown(Viewer viewer, JsonNode? json, IEnumerable<Secret> secrets)
    {
        ArgumentNullException.ThrowIfNull(secrets);
        foreach (var secret in secrets.Where(s => s.HiddenFrom.Includes(viewer)))
        {
            if (string.IsNullOrEmpty(secret.Value))
            {
                throw new ArgumentException("An empty secret would be found everywhere.", nameof(secrets));
            }

            foreach (var path in PathsContaining(json, secret.Value, "$"))
            {
                yield return $"\"{secret.Value}\" (hidden from {secret.HiddenFrom}) found at {path}";
            }
        }
    }

    /// <summary>
    /// The first difference between two JSON documents, located by its path, or <see langword="null"/> when they are the
    /// same. The order of the properties does not count, the order of the items of an array does.
    /// </summary>
    public static string? FirstDifference(JsonNode? one, JsonNode? other, string path = "$")
    {
        switch (one, other)
        {
            case (JsonObject first, JsonObject second):
                foreach (var (name, child) in first)
                {
                    var childPath = Property(path, name);
                    if (!second.TryGetPropertyValue(name, out var otherChild))
                    {
                        return $"{childPath} is only in one state";
                    }

                    if (FirstDifference(child, otherChild, childPath) is { } difference)
                    {
                        return difference;
                    }
                }

                return second.FirstOrDefault(p => !first.ContainsKey(p.Key)).Key is { } extra
                    ? $"{Property(path, extra)} is only in the other state"
                    : null;

            case (JsonArray first, JsonArray second):
                for (var i = 0; i < Math.Min(first.Count, second.Count); i++)
                {
                    if (FirstDifference(first[i], second[i], Index(path, i)) is { } difference)
                    {
                        return difference;
                    }
                }

                return first.Count == second.Count
                    ? null
                    : string.Create(CultureInfo.InvariantCulture, $"{path} has {first.Count} items in one state, {second.Count} in the other");

            default:
                var (shownOne, shownOther) = (Show(one), Show(other));
                return shownOne == shownOther ? null : $"{path} is {shownOne} in one state, {shownOther} in the other";
        }
    }

    private static List<string> PathsContaining(JsonNode? node, string value, string path)
    {
        List<string> paths = [];
        switch (node)
        {
            case JsonObject properties:
                foreach (var (name, child) in properties)
                {
                    var childPath = Property(path, name);

                    // Dictionary keys are data on the wire: a nickname or an identifier may be one.
                    if (name.Contains(value, StringComparison.Ordinal))
                    {
                        paths.Add($"{childPath} (property name)");
                    }

                    paths.AddRange(PathsContaining(child, value, childPath));
                }

                break;

            case JsonArray items:
                for (var i = 0; i < items.Count; i++)
                {
                    paths.AddRange(PathsContaining(items[i], value, Index(path, i)));
                }

                break;

            case JsonValue text when text.GetValueKind() == JsonValueKind.String && text.GetValue<string>().Contains(value, StringComparison.Ordinal):
                paths.Add(path);
                break;
        }

        return paths;
    }

    private static string Show(JsonNode? node)
    {
        var json = node?.ToJsonString(_readable) ?? "null";
        return json.Length <= MaxShownLength ? json : $"{json[..MaxShownLength]}…";
    }

    private static string Property(string path, string name) =>
        name.Length > 0 && !char.IsAsciiDigit(name[0]) && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '$')
            ? $"{path}.{name}"
            : $"{path}['{name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal)}']";

    private static string Index(string path, int index) => string.Create(CultureInfo.InvariantCulture, $"{path}[{index}]");
}
