using System.Buffers;
using System.Collections.Frozen;
using PartyGame.Contracts;

namespace PartyGame.Content;

/// <summary>
/// Checks that each media file referenced by a descriptor is one the server can serve: inside the pack, of a supported
/// type, and present with the exact case of its path.
/// </summary>
/// <remarks>
/// The case is checked on every system: Windows ignores it, but the Raspberry Pi does not, and a pack prepared on a PC must
/// play the same on the Pi.
/// </remarks>
internal static class MediaCheck
{
    /// <summary>
    /// The extensions of the media files supported in phase 2: images, shown on the TV screen.
    /// </summary>
    public static readonly FrozenSet<string> SupportedExtensions =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase, ".jpg", ".jpeg", ".png", ".webp");

    // Refused in file names by Windows, so a pack that uses them on Linux could not be copied to a PC.
    private static readonly SearchValues<char> _forbiddenCharacters = SearchValues.Create("<>:\"|?*");

    public static IEnumerable<PackProblem> Check(string folder, IEnumerable<MediaReference> references)
    {
        foreach (var reference in references)
        {
            foreach (var problem in Check(folder, reference))
            {
                yield return problem;
            }
        }
    }

    private static IEnumerable<PackProblem> Check(string folder, MediaReference reference)
    {
        var media = reference.Media.Value;
        if (IsOutsidePack(media))
        {
            yield return Problem(PackProblemCode.PackMediaOutsidePack, reference);
            yield break;
        }

        var segments = media.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment == "." || segment.Contains('\\', StringComparison.Ordinal)
            || segment.AsSpan().ContainsAny(_forbiddenCharacters) || segment.Any(char.IsControl)))
        {
            yield return Problem(PackProblemCode.PackMediaPathInvalid, reference);
            yield break;
        }

        var extension = Path.GetExtension(segments[^1]);
        if (!SupportedExtensions.Contains(extension))
        {
            yield return Problem(PackProblemCode.PackMediaTypeUnsupported, reference, ("extension", extension));
        }

        if (Find(folder, segments) is not { } actual)
        {
            yield return Problem(PackProblemCode.PackMediaMissing, reference);
        }
        else if (!string.Equals(actual, media, StringComparison.Ordinal))
        {
            yield return Problem(PackProblemCode.PackMediaCaseMismatch, reference, ("actual", actual));
        }
    }

    private static bool IsOutsidePack(string media) =>
        media.StartsWith('/')
        || media.StartsWith('\\')
        || (media.Length >= 2 && char.IsAsciiLetter(media[0]) && media[1] == ':')
        || media.Split('/', '\\').Contains("..", StringComparer.Ordinal);

    /// <returns>
    /// The path of the file, relative to <paramref name="folder"/> with its actual case, or <see langword="null"/> when no
    /// file matches the path, even ignoring case.
    /// </returns>
    private static string? Find(string folder, string[] segments)
    {
        var current = folder;
        var actual = new List<string>(segments.Length);
        for (var index = 0; index < segments.Length; index++)
        {
            var entries = index < segments.Length - 1 ? Directory.EnumerateDirectories(current) : Directory.EnumerateFiles(current);
            var names = entries.Select(Path.GetFileName).ToList();
            var name = names.Find(name => string.Equals(name, segments[index], StringComparison.Ordinal))
                ?? names.Find(name => string.Equals(name, segments[index], StringComparison.OrdinalIgnoreCase));
            if (name is null)
            {
                return null;
            }

            actual.Add(name);
            current = Path.Combine(current, name);
        }

        return string.Join('/', actual);
    }

    private static PackProblem Problem(PackProblemCode code, MediaReference reference, params (string Name, string Value)[] parameters) =>
        Problems.InDescriptor(code, reference.Path, [("media", reference.Media.Value), .. parameters]);
}
