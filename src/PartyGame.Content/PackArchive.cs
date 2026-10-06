using System.Globalization;
using System.IO.Compression;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// Extracts a zip pack to a folder of the cache, to be loaded and served as any pack folder: media files are read with
/// range requests, which the compressed entries of an archive cannot serve.
/// </summary>
internal static class PackArchive
{
    /// <summary>The file extension of a zip pack, compared ignoring case.</summary>
    public const string Extension = ".zip";

    /// <summary>The largest a pack may take once extracted: a zip bomb must not fill the disk of the server.</summary>
    public const long MaxExtractedBytes = 2L * 1024 * 1024 * 1024;

    // Written next to the extracted folder once the extraction is complete: the size and date of the zip it came from.
    private const string StampExtension = ".stamp";

    // The metadata the macOS Finder adds at the root of the zips it creates: not part of the pack.
    private const string MacMetadataFolder = "__MACOSX/";

    /// <summary>
    /// Extracts a zip pack, with <c>pack.json</c> at the root of the folder, unless the folder already holds this very zip
    /// (same size, same date). Checks every entry before writing anything.
    /// </summary>
    /// <param name="archive">The zip file.</param>
    /// <param name="folder">The folder of the cache to extract to, replaced by the extraction.</param>
    /// <param name="maxBytes">The most the extracted files may take: <see cref="MaxExtractedBytes"/>, but in the tests.</param>
    /// <returns>The problem that kept the zip from being extracted, or <see langword="null"/> once extracted.</returns>
    public static PackProblem? Extract(string archive, string folder, long maxBytes = MaxExtractedBytes)
    {
        var info = new FileInfo(archive);
        var stamp = string.Create(CultureInfo.InvariantCulture, $"{info.Length} {info.LastWriteTimeUtc.Ticks}");
        var stampFile = folder + StampExtension;
        if (Directory.Exists(folder) && File.Exists(stampFile) && File.ReadAllText(stampFile) == stamp)
        {
            return null;
        }

        Remove(folder);

        ZipArchive zip;
        try
        {
            zip = ZipFile.OpenRead(archive);
        }
        catch (InvalidDataException)
        {
            return Problem(archive, PackProblemCode.PackArchiveInvalid);
        }

        using (zip)
        {
            var entries = zip.Entries
                .Select(entry => (Entry: entry, Name: entry.FullName.Replace('\\', '/')))
                .Where(entry => !entry.Name.StartsWith(MacMetadataFolder, StringComparison.Ordinal))
                .ToList();

            if (Root(entries.Select(entry => entry.Name)) is not { } root)
            {
                return Problem(archive, PackProblemCode.PackArchiveDescriptorMissing);
            }

            var targets = new List<(ZipArchiveEntry Entry, string Path)>();
            long declared = 0;
            foreach (var (entry, name) in entries)
            {
                var path = Path.GetFullPath(Path.Combine(folder, name[root.Length..]));
                if (!path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal) && path != folder)
                {
                    return Problem(archive, PackProblemCode.PackArchiveEntryOutside, ("entry", entry.FullName));
                }

                declared += entry.Length;
                if (declared > maxBytes)
                {
                    return Problem(archive, PackProblemCode.PackArchiveTooLarge);
                }

                targets.Add((entry, path));
            }

            try
            {
                if (!Write(targets, folder, maxBytes))
                {
                    Remove(folder);
                    return Problem(archive, PackProblemCode.PackArchiveTooLarge);
                }
            }
            catch (InvalidDataException)
            {
                Remove(folder);
                return Problem(archive, PackProblemCode.PackArchiveInvalid);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Remove(folder);
                return Problem(archive, PackProblemCode.PackArchiveExtractionFailed);
            }
        }

        File.WriteAllText(stampFile, stamp);
        return null;
    }

    /// <summary>
    /// Deletes the extracted folders of the zips no longer in the pack directory.
    /// </summary>
    /// <param name="cacheDirectory">The folder of the cache.</param>
    /// <param name="ids">The identifiers of the zip packs still there.</param>
    public static void RemoveOthers(string cacheDirectory, IReadOnlySet<string> ids)
    {
        if (!Directory.Exists(cacheDirectory))
        {
            return;
        }

        foreach (var folder in Directory.EnumerateDirectories(cacheDirectory).Where(folder => !ids.Contains(Path.GetFileName(folder))))
        {
            Remove(folder);
        }

        foreach (var stamp in Directory.EnumerateFiles(cacheDirectory, "*" + StampExtension)
            .Where(stamp => !ids.Contains(Path.GetFileNameWithoutExtension(stamp))))
        {
            File.Delete(stamp);
        }
    }

    // The prefix of the entries where pack.json lies: the root of the zip, or the single folder at its root, which an
    // archiving tool adds when the author compresses the folder of the pack rather than its content.
    private static string? Root(IEnumerable<string> names)
    {
        var all = names.ToHashSet(StringComparer.Ordinal);
        if (all.Contains(PackDescriptor.FileName))
        {
            return "";
        }

        var folders = all.Select(name => name.Split('/')[0]).Distinct(StringComparer.Ordinal).ToList();
        return folders is [var single] && all.Contains($"{single}/{PackDescriptor.FileName}") ? single + "/" : null;
    }

    // Counts the bytes actually written: the sizes an archive declares can lie.
    private static bool Write(List<(ZipArchiveEntry Entry, string Path)> targets, string folder, long maxBytes)
    {
        Directory.CreateDirectory(folder);
        var remaining = maxBytes;
        var buffer = new byte[81920];
        foreach (var (entry, path) in targets)
        {
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                Directory.CreateDirectory(path);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var source = entry.Open();
            using var target = File.Create(path);
            int read;
            while ((read = source.Read(buffer)) > 0)
            {
                remaining -= read;
                if (remaining < 0)
                {
                    return false;
                }

                target.Write(buffer, 0, read);
            }
        }

        return true;
    }

    private static void Remove(string folder)
    {
        // File.Delete refuses a file whose folder does not exist: the cache, before the first zip.
        if (File.Exists(folder + StampExtension))
        {
            File.Delete(folder + StampExtension);
        }

        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static PackProblem Problem(string archive, PackProblemCode code, params (string Name, string Value)[] parameters) =>
        Problems.InFile(code, Path.GetFileName(archive), parameters);
}
