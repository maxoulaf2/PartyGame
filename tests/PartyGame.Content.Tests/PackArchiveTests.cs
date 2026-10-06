using System.IO.Compression;
using System.Text;
using PartyGame.Contracts;
using static PartyGame.Content.Tests.TestPacks;

namespace PartyGame.Content.Tests;

public sealed class PackArchiveTests : IDisposable
{
    private const string Image = "images/drapeau.png";

    private readonly TestPacks _packs = new();

    public void Dispose() => _packs.Dispose();

    private string CacheOf(string id) => Path.Combine(_packs.Cache, id);

    [Theory]
    [InlineData("")]
    [InlineData("album/")]
    public void LoadAll_ZipWithDescriptorAtItsRootOrInItsSingleFolder_IsLoadedAsAFolderNamedAfterTheZip(string prefix)
    {
        // Given
        Zip("album", (prefix + "pack.json", Pack(Quiz(QuestionWithImage(Image)))), (prefix + Image, "png"), ("__MACOSX/._pack.json", ""));

        // When
        var library = Load();

        // Then
        var pack = Assert.Single(library.Packs);
        Assert.True(pack.IsValid, string.Join(Environment.NewLine, Describe(pack)));
        Assert.Equal(("album", Path.Combine(_packs.Root, "album.zip")), (pack.Id, pack.Folder));
        Assert.Equal("png", File.ReadAllText(Path.Combine(CacheOf("album"), "images", "drapeau.png")));
        Assert.False(Directory.Exists(Path.Combine(CacheOf("album"), "__MACOSX")));
    }

    [Fact]
    public void LoadAll_MediaPathInAnotherCaseThanTheEntry_IsReportedAsForAFolder()
    {
        // Given
        Zip("album", ("pack.json", Pack(Quiz(QuestionWithImage(Image)))), ("images/Drapeau.png", "png"));

        // When
        var pack = Assert.Single(Load().Packs);

        // Then
        Assert.Equal(PackProblemCode.PackMediaCaseMismatch, Assert.Single(pack.Problems).Code);
    }

    [Fact]
    public void LoadAll_ZipUnchanged_ReusesItsExtractionAndAChangedZipIsExtractedAgain()
    {
        // Given: a zip extracted once, then a mark left in its extraction
        var archive = Zip("album", ("pack.json", Pack(Quiz(ValidQuestion))));
        Load();
        var mark = Path.Combine(CacheOf("album"), "mark");
        File.WriteAllText(mark, "");

        // When: loaded again, unchanged
        Load();

        // Then
        Assert.True(File.Exists(mark));

        // When: loaded once the zip changed
        File.SetLastWriteTimeUtc(archive, File.GetLastWriteTimeUtc(archive).AddMinutes(1));
        var pack = Assert.Single(Load().Packs);

        // Then
        Assert.False(File.Exists(mark));
        Assert.True(pack.IsValid);
    }

    [Fact]
    public void LoadAll_ZipRemoved_DeletesItsExtraction()
    {
        // Given
        var archive = Zip("album", ("pack.json", Pack(Quiz(ValidQuestion))));
        Load();
        File.Delete(archive);

        // When
        Load();

        // Then
        Assert.Empty(Directory.EnumerateFileSystemEntries(_packs.Cache));
    }

    [Fact]
    public void LoadAll_CorruptZip_IsInvalidAndTheOtherPacksLoad()
    {
        // Given
        _packs.Add("soiree", Pack(Quiz(ValidQuestion)));
        Directory.CreateDirectory(_packs.Root);
        File.WriteAllText(Path.Combine(_packs.Root, "album.zip"), "pas un zip");

        // When
        var library = Load();

        // Then
        Assert.Equal(["PackArchiveInvalid album.zip $"], Describe(library.Packs[0]));
        Assert.Equal("album", library.Packs[0].Id);
        Assert.True(library.Packs[1].IsValid);
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("a/pack.json", "b/notes.txt")]
    [InlineData("a/b/pack.json")]
    public void LoadAll_ZipWithoutDescriptorWhereExpected_IsInvalid(params string[] entries)
    {
        // Given
        Zip("album", [.. entries.Select(entry => (entry, Pack(Quiz(ValidQuestion))))]);

        // When
        var pack = Assert.Single(Load().Packs);

        // Then
        Assert.Equal(["PackArchiveDescriptorMissing album.zip $"], Describe(pack));
    }

    [Theory]
    [InlineData("../evil.txt")]
    [InlineData("images/../../evil.txt")]
    [InlineData("/evil.txt")]
    public void LoadAll_EntryOutOfTheExtractionFolder_IsInvalidAndNothingIsWritten(string entry)
    {
        // Given
        Zip("album", ("pack.json", Pack(Quiz(ValidQuestion))), (entry, "evil"));

        // When
        var pack = Assert.Single(Load().Packs);

        // Then
        Assert.Equal([$"PackArchiveEntryOutside album.zip $ entry={entry}"], Describe(pack));
        Assert.False(Directory.Exists(CacheOf("album")));
        Assert.False(File.Exists(Path.Combine(_packs.Cache, "evil.txt")));
    }

    [Fact]
    public void Extract_ZipLargerThanTheLimitOnceExtracted_IsRefusedWithoutWritingAnything()
    {
        // Given
        var archive = Zip("album", ("pack.json", Pack(Quiz(ValidQuestion))), ("big.bin", new string('0', 1000)));

        // When
        var problem = PackArchive.Extract(archive, CacheOf("album"), maxBytes: 500);

        // Then
        Assert.Equal("PackArchiveTooLarge album.zip $", Describe(problem!));
        Assert.False(Directory.Exists(CacheOf("album")));
    }

    [Fact]
    public void LoadAll_FolderAndZipWithTheSameIdentifier_AreOneInvalidPackInConflict()
    {
        // Given
        _packs.Add("album", Pack(Quiz(ValidQuestion)));
        Zip("album", ("pack.json", Pack(Quiz(ValidQuestion))));

        // When
        var pack = Assert.Single(Load().Packs);

        // Then
        Assert.Equal("album", pack.Id);
        Assert.Equal(["PackIdConflict album.zip $ id=album"], Describe(pack));
    }

    private PackLibrary Load() => new PackLoader(NoModeCheck).LoadAll(_packs.Root, _packs.Cache);

    private string Zip(string id, params (string Name, string Content)[] entries)
    {
        Directory.CreateDirectory(_packs.Root);
        var archive = Path.Combine(_packs.Root, id + ".zip");
        using var zip = ZipFile.Open(archive, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using var stream = zip.CreateEntry(name).Open();
            stream.Write(Encoding.UTF8.GetBytes(content));
        }

        return archive;
    }
}
