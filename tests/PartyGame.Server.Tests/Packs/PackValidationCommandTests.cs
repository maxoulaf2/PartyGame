using PartyGame.Contracts;
using PartyGame.Server.Packs;
using PartyGame.Tests.Shared;

namespace PartyGame.Server.Tests.Packs;

public sealed class PackValidationCommandTests : IDisposable
{
    private readonly TempDirectory _packs = new();

    public void Dispose() => _packs.Dispose();

    [Fact]
    public void Run_ValidPack_ListsItsRoundsAndExitsWithZero()
    {
        var folder = WritePack("valide", TestPacks.Quiz("Pack valide", "Première manche", "Seconde manche"));

        var (code, output) = Run(folder);

        Assert.Equal(PackValidationCommand.Valid, code);
        Assert.Contains("Pack « Pack valide »", output, StringComparison.Ordinal);
        Assert.Contains("Manche 1 : Première manche (quiz)", output, StringComparison.Ordinal);
        Assert.Contains("Manche 2 : Seconde manche (quiz)", output, StringComparison.Ordinal);
        Assert.Contains("Pack valide", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_InvalidPack_DescribesEachProblemInFrenchAndExitsWithOne()
    {
        var folder = WritePack("invalide", TestPacks.Broken("Pack cassé"));

        var (code, output) = Run(folder);

        Assert.Equal(PackValidationCommand.Invalid, code);
        Assert.Contains(
            "pack.json, $.rounds[0].questions[0] : Question sans bonne réponse : marquez une proposition avec \"correct\": true [QuizCorrectChoiceMissing]",
            output,
            StringComparison.Ordinal);
        Assert.Contains("Pack invalide : 1 problème", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_FolderOfPacks_ChecksEachAndSummarizes()
    {
        WritePack("a", TestPacks.Quiz("A", "Manche"));
        WritePack("b", TestPacks.Quiz("B", "Manche"));
        WritePack("c", TestPacks.Broken("C"));

        var (code, output) = Run(_packs.Path);

        Assert.Equal(PackValidationCommand.Invalid, code);
        Assert.Contains("2 packs valides, 1 invalide", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_RepositoryPacks_AreAllValid()
    {
        var (code, output) = Run(Path.Combine(RepositoryRoot.Find(), "packs"));

        Assert.Equal(PackValidationCommand.Valid, code);
        Assert.Contains(" packs valides, 0 invalide", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_MissingPath_ExitsWithTwo()
    {
        var (code, output) = Run(Path.Combine(_packs.Path, "absent"));

        Assert.Equal(PackValidationCommand.NotFound, code);
        Assert.Contains("introuvable", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_FolderWithoutPack_ExitsWithTwo()
    {
        Directory.CreateDirectory(Path.Combine(_packs.Path, "vide"));

        var (code, output) = Run(_packs.Path);

        Assert.Equal(PackValidationCommand.NotFound, code);
        Assert.Contains("ni un pack ni un dossier de packs", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Messages_EveryProblemCode_HasAFrenchMessage()
    {
        Assert.All(Enum.GetValues<PackProblemCode>(), code => Assert.True(PackProblemMessages.Messages.ContainsKey(code), $"{code} has no message"));
    }

    private string WritePack(string id, string descriptor)
    {
        var folder = Path.Combine(_packs.Path, id);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "pack.json"), descriptor);
        return folder;
    }

    private static (int Code, string Output) Run(string path)
    {
        using var output = new StringWriter();
        var code = PackValidationCommand.Run([path], output);
        return (code, output.ToString());
    }
}
