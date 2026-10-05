using System.Buffers;
using System.Collections.Frozen;
using System.Globalization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// Checks that each media file referenced by a descriptor is one the server can serve: inside the pack, of the type its
/// property expects, present with the exact case of its path and, for an audio file, readable. Then checks that each audio
/// excerpt starts before the end of its track.
/// </summary>
/// <remarks>
/// The case is checked on every system: Windows ignores it, but the Raspberry Pi does not, and a pack prepared on a PC must
/// play the same on the Pi.
/// </remarks>
internal static class MediaCheck
{
    /// <summary>
    /// The extensions of the images, shown on the TV screen.
    /// </summary>
    public static readonly FrozenSet<string> ImageExtensions =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase, ".jpg", ".jpeg", ".png", ".webp");

    /// <summary>
    /// The extensions of the audio files, played by the TV screen.
    /// </summary>
    public static readonly FrozenSet<string> AudioExtensions = FrozenSet.Create(StringComparer.OrdinalIgnoreCase, ".mp3");

    // Refused in file names by Windows, so a pack that uses them on Linux could not be copied to a PC.
    private static readonly SearchValues<char> _forbiddenCharacters = SearchValues.Create("<>:\"|?*");

    public static IEnumerable<PackProblem> Check(string folder, DescriptorReading reading)
    {
        // The audio files without problem, with their duration: each one is read once, however many excerpts use it.
        var durations = new Dictionary<MediaPath, TimeSpan>();
        foreach (var reference in reading.Media)
        {
            foreach (var problem in Check(folder, reference, durations))
            {
                yield return problem;
            }
        }

        foreach (var (path, excerpt) in reading.Excerpts)
        {
            if (durations.TryGetValue(excerpt.File, out var duration) && excerpt.Start >= duration.TotalSeconds)
            {
                yield return Problems.InDescriptor(
                    PackProblemCode.PackAudioExcerptStartBeyondEnd,
                    JsonPath.Property(path, "start"),
                    ("start", excerpt.Start.ToString(CultureInfo.InvariantCulture)),
                    ("duration", Math.Round(duration.TotalSeconds, 1).ToString(CultureInfo.InvariantCulture)));
            }
        }
    }

    private static IEnumerable<PackProblem> Check(string folder, MediaReference reference, Dictionary<MediaPath, TimeSpan> durations)
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
        var supported = (reference.IsAudio ? AudioExtensions : ImageExtensions).Contains(extension);
        if (!supported)
        {
            yield return Problem(
                PackProblemCode.PackMediaTypeUnsupported,
                reference,
                ("extension", extension),
                ("expected", reference.IsAudio ? "audio" : "image"));
        }

        if (Find(folder, segments) is not { } actual)
        {
            yield return Problem(PackProblemCode.PackMediaMissing, reference);
        }
        else if (!string.Equals(actual, media, StringComparison.Ordinal))
        {
            yield return Problem(PackProblemCode.PackMediaCaseMismatch, reference, ("actual", actual));
        }
        else if (reference.IsAudio && supported && !durations.ContainsKey(reference.Media))
        {
            if (Mp3File.Read(Path.Combine([folder, .. segments])) is { } mp3)
            {
                durations[reference.Media] = mp3.Duration;
            }
            else
            {
                yield return Problem(PackProblemCode.PackMediaUnreadable, reference);
            }
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
