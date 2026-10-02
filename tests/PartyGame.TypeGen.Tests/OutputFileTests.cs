namespace PartyGame.TypeGen.Tests;

public sealed class OutputFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"typegen-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(_directory, "nested", "schema.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void FindDifference_MissingFile_ReportsMissing() =>
        Assert.Equal("schema.json is missing", OutputFile.FindDifference(FilePath, "{}\n", "schema.json"));

    [Fact]
    public void FindDifference_AfterWrite_ReportsNothing()
    {
        OutputFile.Write(FilePath, "{}\n");

        Assert.Null(OutputFile.FindDifference(FilePath, "{}\n", "schema.json"));
    }

    [Fact]
    public void FindDifference_ChangedFile_ReportsOutOfDate()
    {
        OutputFile.Write(FilePath, "{}\n");
        File.WriteAllText(FilePath, "{ \"edited\": true }\n");

        Assert.Equal("schema.json is out of date", OutputFile.FindDifference(FilePath, "{}\n", "schema.json"));
    }

    [Fact]
    public void FindDifference_CrlfCheckout_ReportsNothing()
    {
        OutputFile.Write(FilePath, "{\n}\n");
        File.WriteAllText(FilePath, "{\r\n}\r\n");

        Assert.Null(OutputFile.FindDifference(FilePath, "{\n}\n", "schema.json"));
    }

    [Fact]
    public void Write_UpToDateFile_LeavesItUntouched()
    {
        OutputFile.Write(FilePath, "{\n}\n");
        File.WriteAllText(FilePath, "{\r\n}\r\n");

        OutputFile.Write(FilePath, "{\n}\n");

        Assert.Equal("{\r\n}\r\n", File.ReadAllText(FilePath));
    }
}
