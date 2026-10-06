using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// Loads the packs and checks them entirely, so that no game ever fails because of its content: structure of the
/// descriptor, simple constraints, consistency of each activity for its game mode, and media files.
/// </summary>
/// <param name="validateRound">The consistency check of the game modes, provided by the server.</param>
public sealed class PackLoader(RoundValidator validateRound)
{
    private static readonly JsonDocumentOptions _documentOptions = new()
    {
        // The same standard JSON as the deserialization (PackJsonOptions): no comment, no trailing comma.
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    private static readonly string _titleProperty = PackJsonOptions.Default.PropertyNamingPolicy!.ConvertName(nameof(PackDescriptor.Title));
    private static readonly string _roundsProperty = PackJsonOptions.Default.PropertyNamingPolicy!.ConvertName(nameof(PackDescriptor.Rounds));

    /// <summary>
    /// Loads each subfolder of the pack directory that holds a <c>pack.json</c>, and each zip file, extracted to the cache;
    /// ignores the rest. A folder and a zip, or two zips, with the same identifier are reported as one pack in
    /// <see cref="PackProblemCode.PackIdConflict"/>. A pack that fails to load because of a bug is reported as
    /// <see cref="PackProblemCode.PackLoadFailed"/>, with the <see cref="LoadedPack.Failure"/> to log, and the other packs
    /// load as usual.
    /// </summary>
    /// <param name="directory">The pack directory, which may not exist.</param>
    /// <param name="cacheDirectory">The folder the zip packs are extracted to, created when needed. The folders of the zips no
    /// longer in the pack directory are deleted from it.</param>
    public PackLibrary LoadAll(string directory, string cacheDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheDirectory);

        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        if (!Directory.Exists(fullPath))
        {
            return new PackLibrary(fullPath, DirectoryExists: false, []);
        }

        var packs = ImmutableArray.CreateBuilder<LoadedPack>();
        var sources = new List<(string Id, string Path, bool IsArchive)>();
        foreach (var folder in Directory.EnumerateDirectories(fullPath))
        {
            try
            {
                if (HasDescriptor(folder))
                {
                    sources.Add((Path.GetFileName(folder), folder, false));
                }
            }
            catch (Exception ex)
            {
                packs.Add(Failed(Path.GetFileName(folder), folder, ex));
            }
        }

        sources.AddRange(Directory.EnumerateFiles(fullPath)
            .Where(file => string.Equals(Path.GetExtension(file), PackArchive.Extension, StringComparison.OrdinalIgnoreCase))
            .Select(file => (Path.GetFileNameWithoutExtension(file), file, true)));

        var fullCache = Path.TrimEndingDirectorySeparator(Path.GetFullPath(cacheDirectory));
        PackArchive.RemoveOthers(fullCache, sources.Where(source => source.IsArchive).Select(source => source.Id).ToHashSet(StringComparer.Ordinal));

        foreach (var group in sources.GroupBy(source => source.Id, StringComparer.Ordinal))
        {
            if (group.Count() > 1)
            {
                packs.Add(new LoadedPack(
                    group.Key,
                    Path.Combine(fullPath, group.Key),
                    Title: null,
                    RoundCount: null,
                    Descriptor: null,
                    [.. group.Where(source => source.IsArchive)
                        .Select(source => Problems.InFile(PackProblemCode.PackIdConflict, Path.GetFileName(source.Path), ("id", group.Key)))]));
                continue;
            }

            var (id, path, isArchive) = group.Single();
            try
            {
                packs.Add(isArchive ? LoadArchive(path, fullCache) : Load(path));
            }
            catch (Exception ex)
            {
                packs.Add(Failed(id, path, ex));
            }
        }

        packs.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        return new PackLibrary(fullPath, DirectoryExists: true, packs.ToImmutable());
    }

    /// <summary>
    /// Extracts a zip pack to the cache, unless already done for this very zip, then loads it as a folder. Its identifier is
    /// the name of the zip without its extension, and its <see cref="LoadedPack.Folder"/> the zip, which the author knows.
    /// </summary>
    /// <param name="archive">The zip file.</param>
    /// <param name="cacheDirectory">The folder the zip packs are extracted to.</param>
    /// <exception cref="IOException">The zip, the cache or a file of the pack cannot be read.</exception>
    public LoadedPack LoadArchive(string archive, string cacheDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheDirectory);

        var fullPath = Path.GetFullPath(archive);
        var id = Path.GetFileNameWithoutExtension(fullPath);
        var folder = Path.Combine(Path.TrimEndingDirectorySeparator(Path.GetFullPath(cacheDirectory)), id);
        return PackArchive.Extract(fullPath, folder) is { } problem
            ? new LoadedPack(id, fullPath, Title: null, RoundCount: null, Descriptor: null, [problem])
            : Load(folder) with { Folder = fullPath };
    }

    /// <summary>
    /// Loads a pack and reports every problem at once. Only a JSON syntax error stops the checks, since nothing can be read
    /// past it.
    /// </summary>
    /// <param name="folder">The folder of the pack, which holds a <c>pack.json</c>.</param>
    /// <exception cref="IOException">The descriptor or a folder of the pack cannot be read.</exception>
    public LoadedPack Load(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        var id = Path.GetFileName(fullPath);
        var text = File.ReadAllText(Path.Combine(fullPath, PackDescriptor.FileName));

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text, _documentOptions);
        }
        catch (JsonException ex)
        {
            return new LoadedPack(id, fullPath, Title: null, RoundCount: null, Descriptor: null, [SyntaxProblem(ex, text)]);
        }

        using (document)
        {
            var root = document.RootElement;
            var reading = DescriptorReader.Read(root);

            var problems = reading.Problems.ToBuilder();
            foreach (var round in reading.Rounds)
            {
                problems.AddRange(validateRound(round.Round, round.Path));
            }

            problems.AddRange(MediaCheck.Check(fullPath, reading));

            // Every check passed, so the deserialization cannot fail but because of a bug.
            var descriptor = problems.Count == 0 ? root.Deserialize<PackDescriptor>(PackJsonOptions.Default) : null;
            return new LoadedPack(id, fullPath, Title(root), RoundCount(root), descriptor, problems.ToImmutable())
            {
                Media = descriptor is null ? [] : [.. reading.Media.Select(reference => reference.Media).Distinct()],
            };
        }
    }

    /// <summary>
    /// Whether a folder holds a descriptor, named exactly <c>pack.json</c>: on Windows, a search pattern ignores case, where
    /// the Raspberry Pi would not find a <c>Pack.json</c>.
    /// </summary>
    public static bool HasDescriptor(string folder) =>
        Directory.EnumerateFiles(folder).Any(file => string.Equals(Path.GetFileName(file), PackDescriptor.FileName, StringComparison.Ordinal));

    private static LoadedPack Failed(string id, string path, Exception failure) =>
        new(id, path, Title: null, RoundCount: null, Descriptor: null, [Problems.InDescriptor(PackProblemCode.PackLoadFailed, JsonPath.Root)])
        {
            Failure = failure,
        };

    private static PackProblem SyntaxProblem(JsonException exception, string text)
    {
        var line = exception.LineNumber ?? 0;
        var bytePosition = exception.BytePositionInLine ?? 0;
        return Problems.InDescriptor(
            PackProblemCode.PackJsonInvalid,
            JsonPath.Root,
            ("line", (line + 1).ToString(CultureInfo.InvariantCulture)),
            ("column", Column(text, line, bytePosition).ToString(CultureInfo.InvariantCulture)));
    }

    // The reader counts bytes of UTF-8, where an editor counts characters: an accent before the error would shift it.
    private static long Column(string text, long line, long bytePosition)
    {
        var lines = text.Split('\n');
        if (line >= lines.Length)
        {
            return bytePosition + 1;
        }

        var bytes = Encoding.UTF8.GetBytes(lines[line]);
        return Encoding.UTF8.GetCharCount(bytes, 0, (int)Math.Min(bytePosition, bytes.Length)) + 1;
    }

    private static string? Title(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(_titleProperty, out var title) && title.ValueKind == JsonValueKind.String
            ? title.GetString()
            : null;

    private static int? RoundCount(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(_roundsProperty, out var rounds) && rounds.ValueKind == JsonValueKind.Array
            ? rounds.GetArrayLength()
            : null;
}
