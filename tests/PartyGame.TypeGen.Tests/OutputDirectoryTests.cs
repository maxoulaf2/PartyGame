namespace PartyGame.TypeGen.Tests;

public sealed class OutputDirectoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"typegen-{Guid.NewGuid():N}");

    private static readonly GeneratedFile[] _files = [new("A.ts", "a\n"), new("index.ts", "index\n")];

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void FindDifferences_MissingDirectory_ReportsEveryFileMissing()
    {
        var differences = OutputDirectory.FindDifferences(_directory, _files);

        Assert.Equal(["A.ts is missing", "index.ts is missing"], differences);
    }

    [Fact]
    public void FindDifferences_AfterWrite_ReportsNothing()
    {
        OutputDirectory.Write(_directory, _files);

        Assert.Empty(OutputDirectory.FindDifferences(_directory, _files));
    }

    [Fact]
    public void FindDifferences_ChangedAndStaleFiles_ReportsBoth()
    {
        OutputDirectory.Write(_directory, _files);
        File.WriteAllText(Path.Combine(_directory, "A.ts"), "edited by hand\n");
        File.WriteAllText(Path.Combine(_directory, "Removed.ts"), "old\n");

        var differences = OutputDirectory.FindDifferences(_directory, _files);

        Assert.Equal(["A.ts is out of date", "Removed.ts is no longer generated"], differences);
    }

    [Fact]
    public void FindDifferences_CrlfCheckout_ReportsNothing()
    {
        OutputDirectory.Write(_directory, _files);
        File.WriteAllText(Path.Combine(_directory, "A.ts"), "a\r\n");

        Assert.Empty(OutputDirectory.FindDifferences(_directory, _files));
    }

    [Fact]
    public void Write_StaleFiles_DeletesOnlyTypeScriptFiles()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "Removed.ts"), "old\n");
        File.WriteAllText(Path.Combine(_directory, "README.md"), "kept\n");

        OutputDirectory.Write(_directory, _files);

        Assert.Equal(
            ["A.ts", "README.md", "index.ts"],
            Directory.EnumerateFiles(_directory).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }
}
