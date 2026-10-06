using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using static PartyGame.Content.Tests.TestPacks;

namespace PartyGame.Content.Tests;

public sealed class PackLibraryTests : IDisposable
{
    private readonly TestPacks _packs = new();

    public void Dispose() => _packs.Dispose();

    [Fact]
    public void LoadAll_SubfoldersWithAndWithoutDescriptor_LoadsOnlyThePacksInOrderOfFolderName()
    {
        // Given
        _packs.Add("b-second", Pack(Quiz(ValidQuestion)));
        _packs.Add("a-first", Pack(Quiz(ValidQuestion), Quiz(ValidQuestion)));
        Directory.CreateDirectory(Path.Combine(_packs.Root, "not-a-pack"));
        File.WriteAllText(Path.Combine(_packs.Root, "not-a-pack", "notes.txt"), "Pas un pack");

        // When
        var library = new PackLoader(NoModeCheck).LoadAll(_packs.Root, _packs.Cache);

        // Then
        Assert.True(library.DirectoryExists);
        Assert.Equal(Path.GetFullPath(_packs.Root), library.Directory);
        Assert.Equal(["a-first", "b-second"], library.Packs.Select(pack => pack.Id));
        Assert.All(library.Packs, pack => Assert.True(pack.IsValid));
        Assert.Equal(Path.Combine(_packs.Root, "a-first"), library.Packs[0].Folder);
        Assert.Equal(2, library.Packs[0].RoundCount);
    }

    [Fact]
    public void LoadAll_DescriptorNameInAnotherCase_IgnoresTheFolderOnEverySystem()
    {
        // Given
        var folder = _packs.Add("pack", Pack(Quiz(ValidQuestion)));
        File.Move(Path.Combine(folder, PackDescriptor.FileName), Path.Combine(folder, "Pack.json"));

        // When
        var library = new PackLoader(NoModeCheck).LoadAll(_packs.Root, _packs.Cache);

        // Then
        Assert.Empty(library.Packs);
    }

    [Fact]
    public void LoadAll_MissingDirectory_ReturnsNoPackAndSaysTheDirectoryIsMissing()
    {
        // When
        var library = new PackLoader(NoModeCheck).LoadAll(Path.Combine(_packs.Root, "missing"), _packs.Cache);

        // Then
        Assert.False(library.DirectoryExists);
        Assert.Empty(library.Packs);
    }

    [Fact]
    public void LoadAll_EmptyDirectory_ReturnsNoPack()
    {
        // Given
        Directory.CreateDirectory(_packs.Root);

        // When
        var library = new PackLoader(NoModeCheck).LoadAll(_packs.Root, _packs.Cache);

        // Then
        Assert.True(library.DirectoryExists);
        Assert.Empty(library.Packs);
    }

    [Fact]
    public void LoadAll_ModeCheckThrows_MarksThePackFailedAndLoadsTheOthers()
    {
        // Given
        _packs.Add("a-broken", Pack("""{ "type": "quiz", "title": "Bug", "questions": [] }"""));
        _packs.Add("b-fine", Pack(Quiz(ValidQuestion)));
        var failure = new InvalidOperationException("Bug in a mode");
        var loader = new PackLoader((round, path) => round.Title == "Bug" ? throw failure : []);

        // When
        var library = loader.LoadAll(_packs.Root, _packs.Cache);

        // Then
        var broken = library.Packs[0];
        Assert.False(broken.IsValid);
        Assert.Same(failure, broken.Failure);
        Assert.Equal(["PackLoadFailed pack.json $"], Describe(broken));
        Assert.True(library.Packs[1].IsValid);
        Assert.Null(library.Packs[1].Failure);
    }

    [Fact]
    public void LoadAll_ValidPack_KeepsItsDescriptorWithoutReadingTheFileAgain()
    {
        // Given
        var folder = _packs.Add("pack", Pack(Quiz(ValidQuestion)));

        // When
        var library = new PackLoader(NoModeCheck).LoadAll(_packs.Root, _packs.Cache);
        File.Delete(Path.Combine(folder, PackDescriptor.FileName));

        // Then
        var pack = Assert.Single(library.Packs);
        Assert.True(pack.IsValid);
        Assert.Equal("Pack de test", pack.Descriptor.Title);
        Assert.Equal("Manche", Assert.Single(pack.Descriptor.Rounds).Title);
    }

    [Fact]
    public void Load_ValidPack_HasNoProblem()
    {
        // When
        var pack = _packs.Load(Pack(Quiz(ValidQuestion)));

        // Then
        Assert.True(pack.IsValid);
        Assert.Empty(pack.Problems);
        Assert.Equal("pack", pack.Id);
        Assert.Equal("Pack de test", pack.Title);
        Assert.Equal(1, pack.RoundCount);
        Assert.IsType<QuizRoundDescriptor>(Assert.Single(pack.Descriptor.Rounds));
    }

    [Fact]
    public void Load_InvalidPack_KeepsItsTitleAndRoundCountButNoDescriptor()
    {
        // When
        var pack = _packs.Load(Pack(Quiz(ValidQuestion), """{ "type": "quiz" }"""));

        // Then
        Assert.False(pack.IsValid);
        Assert.Null(pack.Descriptor);
        Assert.Equal("Pack de test", pack.Title);
        Assert.Equal(2, pack.RoundCount);
    }

    [Fact]
    public void Load_ProblemsOfTheMode_AreReportedUnderThePathOfEachRound()
    {
        // Given
        var paths = new List<string>();
        ImmutableArray<PackProblem> Check(RoundDescriptor round, string path)
        {
            paths.Add(path);
            return [new PackProblem(PackProblemCode.QuizCorrectChoiceMissing, PackDescriptor.FileName, $"{path}.questions[0]", ImmutableDictionary<string, string>.Empty)];
        }

        // When
        var pack = new PackLoader(Check).Load(_packs.Add("pack", Pack(Quiz(ValidQuestion), Quiz(ValidQuestion))));

        // Then
        Assert.Equal(["$.rounds[0]", "$.rounds[1]"], paths);
        Assert.Equal(
            ["QuizCorrectChoiceMissing pack.json $.rounds[0].questions[0]", "QuizCorrectChoiceMissing pack.json $.rounds[1].questions[0]"],
            Describe(pack));
        Assert.False(pack.IsValid);
    }

    [Fact]
    public void Load_RoundThatCannotBeRead_IsNotCheckedByItsModeUnlikeTheOthers()
    {
        // Given
        var checkedTitles = new List<string>();
        var loader = new PackLoader((round, path) =>
        {
            checkedTitles.Add(round.Title);
            return [];
        });
        var unreadable = """{ "type": "quiz", "title": "Illisible", "questions": [{ "text": 3, "choices": [] }] }""";
        var outOfBounds = """{ "type": "quiz", "title": "Hors bornes", "answerSeconds": 1, "questions": [] }""";

        // When
        var pack = loader.Load(_packs.Add("pack", Pack(unreadable, Quiz(ValidQuestion), outOfBounds)));

        // Then
        Assert.Equal(["Manche", "Hors bornes"], checkedTitles);
        Assert.False(pack.IsValid);
    }
}
