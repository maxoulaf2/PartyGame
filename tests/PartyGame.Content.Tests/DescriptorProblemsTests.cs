using PartyGame.Contracts;
using static PartyGame.Content.Tests.TestPacks;

namespace PartyGame.Content.Tests;

public sealed class DescriptorProblemsTests : IDisposable
{
    private readonly TestPacks _packs = new();

    public void Dispose() => _packs.Dispose();

    [Fact]
    public void Load_MissingComma_GivesTheLineAndColumnOfTheError()
    {
        // Given
        const string Descriptor = """
            {
              "formatVersion": 1
              "title": "Pack de test",
              "rounds": []
            }
            """;

        // When
        var pack = _packs.Load(Descriptor);

        // Then
        Assert.Equal(["PackJsonInvalid pack.json $ column=3 line=3"], Describe(pack));
        Assert.Null(pack.Title);
        Assert.Null(pack.RoundCount);
    }

    [Fact]
    public void Load_SyntaxErrorAfterAccents_CountsTheColumnInCharacters()
    {
        // When
        var pack = _packs.Load("""{ "title": "Été" ] }""");

        // Then
        Assert.Equal(["PackJsonInvalid pack.json $ column=18 line=1"], Describe(pack));
    }

    [Theory]
    [InlineData("""{ "formatVersion": 1, }""")]
    [InlineData("""{ "formatVersion": 1 /* version */ }""")]
    [InlineData("")]
    public void Load_NotStandardJson_IsASyntaxError(string descriptor)
    {
        // When
        var pack = _packs.Load(descriptor);

        // Then
        Assert.Equal(PackProblemCode.PackJsonInvalid, Assert.Single(pack.Problems).Code);
    }

    [Theory]
    [InlineData("""{ "formatVersion": 1, "rounds": [R] }""", "PackPropertyMissing pack.json $ property=title")]
    [InlineData("""{ "title": "Pack", "rounds": [R] }""", "PackPropertyMissing pack.json $ property=formatVersion")]
    [InlineData("""{ "formatVersion": 1, "title": "Pack", "rounds": [{ "type": "quiz", "title": "Manche", "questions": [{ "text": "Question ?" }] }] }""",
        "PackPropertyMissing pack.json $.rounds[0].questions[0] property=choices")]
    public void Load_RequiredPropertyMissing_GivesThePathOfTheObject(string descriptor, string problem)
    {
        // When
        var pack = _packs.Load(WithRound(descriptor));

        // Then
        Assert.Equal([problem], Describe(pack));
    }

    [Theory]
    [InlineData("""{ "formatVersion": 1, "title": "Pack", "rounds": [], "auteur": "Moi" }""", "PackPropertyUnknown pack.json $.auteur property=auteur")]
    [InlineData("""{ "formatVersion": 1, "Title": "Pack", "title": "Pack", "rounds": [] }""", "PackPropertyUnknown pack.json $.Title property=Title")]
    [InlineData("""{ "formatVersion": 1, "title": "Pack", "rounds": [{ "type": "quiz", "title": "Manche", "question": [] }] }""",
        "PackPropertyUnknown pack.json $.rounds[0].question property=question")]
    [InlineData("""{ "formatVersion": 1, "title": "Pack", "rounds": [], "nom du pack": "Pack" }""",
        "PackPropertyUnknown pack.json $['nom du pack'] property=nom du pack")]
    public void Load_UnknownProperty_GivesThePathOfTheProperty(string descriptor, string problem)
    {
        // When
        var pack = _packs.Load(descriptor);

        // Then
        Assert.Contains(problem, Describe(pack));
    }

    [Theory]
    [InlineData("""{ "formatVersion": 1, "title": 3, "rounds": [] }""", "$.title", "string")]
    [InlineData("""{ "formatVersion": 1, "title": null, "rounds": [] }""", "$.title", "string")]
    [InlineData("""{ "formatVersion": "1", "title": "Pack", "rounds": [] }""", "$.formatVersion", "integer")]
    [InlineData("""{ "formatVersion": 1.5, "title": "Pack", "rounds": [] }""", "$.formatVersion", "integer")]
    [InlineData("""{ "formatVersion": 1, "title": "Pack", "rounds": {} }""", "$.rounds", "array")]
    [InlineData("""{ "formatVersion": 1, "title": "Pack", "rounds": [3] }""", "$.rounds[0]", "object")]
    [InlineData("""{ "formatVersion": 1, "title": "Pack", "rounds": [null] }""", "$.rounds[0]", "object")]
    [InlineData("""[]""", "$", "object")]
    public void Load_ValueOfTheWrongType_GivesItsPathAndTheExpectedType(string descriptor, string path, string expected)
    {
        // When
        var pack = _packs.Load(descriptor);

        // Then
        Assert.Contains($"PackValueTypeInvalid pack.json {path} expected={expected}", Describe(pack));
    }

    [Theory]
    [InlineData("""{ "text": "Question ?", "answerSeconds": 20.5, "choices": [{ "text": "Oui", "correct": true }, { "text": "Non" }] }""", "$.rounds[0].questions[0].answerSeconds", "integer")]
    [InlineData("""{ "text": "Question ?", "image": 3, "choices": [{ "text": "Oui", "correct": true }, { "text": "Non" }] }""", "$.rounds[0].questions[0].image", "string")]
    [InlineData("""{ "text": "Question ?", "choices": [{ "text": "Oui", "correct": "oui" }, { "text": "Non" }] }""", "$.rounds[0].questions[0].choices[0].correct", "boolean")]
    [InlineData("""{ "text": "Question ?", "choices": [null, { "text": "Non" }] }""", "$.rounds[0].questions[0].choices[0]", "object")]
    [InlineData("""{ "text": "Question ?", "choices": [{ "text": "Oui", "correct": null }, { "text": "Non" }] }""", "$.rounds[0].questions[0].choices[0].correct", "boolean")]
    public void Load_ValueOfTheWrongTypeInAQuestion_GivesItsPathAndTheExpectedType(string question, string path, string expected)
    {
        // When
        var pack = _packs.Load(Pack(Quiz(question)));

        // Then
        Assert.Equal([$"PackValueTypeInvalid pack.json {path} expected={expected}"], Describe(pack));
    }

    [Fact]
    public void Load_NullOptionalValues_AreAccepted()
    {
        // Given
        var question = """{ "text": "Question ?", "image": null, "answerSeconds": null, "choices": [{ "text": "Oui", "correct": true }, { "text": "Non" }] }""";

        // When
        var pack = _packs.Load($$"""{ "formatVersion": 1, "title": "Pack", "description": null, "rounds": [{{Quiz(question)}}] }""");

        // Then
        Assert.Empty(pack.Problems);
    }

    [Theory]
    [InlineData("""{ "type": "karaoke", "title": "Manche" }""", "PackRoundTypeUnknown pack.json $.rounds[0].type type=karaoke")]
    [InlineData("""{ "type": "Quiz", "title": "Manche" }""", "PackRoundTypeUnknown pack.json $.rounds[0].type type=Quiz")]
    [InlineData("""{ "title": "Manche", "questions": [] }""", "PackPropertyMissing pack.json $.rounds[0] property=type")]
    [InlineData("""{ "type": 1, "title": "Manche" }""", "PackValueTypeInvalid pack.json $.rounds[0].type expected=string")]
    public void Load_RoundTypeMissingOrUnknown_IsReportedOnce(string round, string problem)
    {
        // When
        var pack = _packs.Load(Pack(round));

        // Then
        Assert.Equal([problem], Describe(pack));
    }

    [Fact]
    public void Load_TypeAfterTheOtherProperties_IsRead()
    {
        // When
        var pack = _packs.Load(Pack($$"""{ "title": "Manche", "questions": [{{ValidQuestion}}], "type": "quiz" }"""));

        // Then
        Assert.True(pack.IsValid);
    }

    [Theory]
    [InlineData("""{ "formatVersion": 2, "title": "Pack", "rounds": [R] }""", "PackValueOutOfRange pack.json $.formatVersion max=1 min=1")]
    [InlineData("""{ "formatVersion": 1, "title": "", "rounds": [R] }""", "PackTextLengthOutOfRange pack.json $.title max=60 min=1")]
    [InlineData("""{ "formatVersion": 1, "title": "Pack", "rounds": [] }""", "PackItemCountOutOfRange pack.json $.rounds min=1")]
    public void Load_ValueOutOfBounds_GivesItsPathAndBounds(string descriptor, string problem)
    {
        // When
        var pack = _packs.Load(WithRound(descriptor));

        // Then
        Assert.Equal([problem], Describe(pack));
    }

    [Theory]
    [InlineData("""{ "type": "quiz", "title": "Manche", "answerSeconds": 4, "questions": [Q] }""", "PackValueOutOfRange pack.json $.rounds[0].answerSeconds max=120 min=5")]
    [InlineData("""{ "type": "quiz", "title": "Manche", "points": 10001, "questions": [Q] }""", "PackValueOutOfRange pack.json $.rounds[0].points max=10000 min=0")]
    [InlineData("""{ "type": "quiz", "title": "Manche", "questions": [] }""", "PackItemCountOutOfRange pack.json $.rounds[0].questions max=50 min=1")]
    [InlineData("""{ "type": "quiz", "title": "Manche", "description": "", "questions": [Q] }""", "PackTextLengthOutOfRange pack.json $.rounds[0].description max=300 min=1")]
    [InlineData("""{ "type": "quiz", "title": "Manche", "questions": [{ "text": "Question ?", "choices": [{ "text": "Oui", "correct": true }] }] }""",
        "PackItemCountOutOfRange pack.json $.rounds[0].questions[0].choices max=4 min=2")]
    [InlineData("""{ "type": "quiz", "title": "Manche", "questions": [{ "text": "Question ?", "choices": [{ "text": "", "correct": true }, { "text": "Non" }] }] }""",
        "PackTextLengthOutOfRange pack.json $.rounds[0].questions[0].choices[0].text max=80 min=1")]
    public void Load_RoundValueOutOfBounds_GivesItsPathAndBounds(string round, string problem)
    {
        // When
        var pack = _packs.Load(Pack(round.Replace("[Q]", $"[{ValidQuestion}]", StringComparison.Ordinal)));

        // Then
        Assert.Equal([problem], Describe(pack));
    }

    [Theory]
    [InlineData(300, true)]
    [InlineData(301, false)]
    public void Load_DescriptionOfARound_IsLimitedTo300Characters(int length, bool valid)
    {
        // Given
        var round = $$"""{ "type": "quiz", "title": "Manche", "description": "{{new string('é', length)}}", "questions": [{{ValidQuestion}}] }""";

        // When
        var pack = _packs.Load(Pack(round));

        // Then
        Assert.Equal(valid ? [] : ["PackTextLengthOutOfRange pack.json $.rounds[0].description max=300 min=1"], Describe(pack));
    }

    [Fact]
    public void Load_SeveralProblems_ReportsThemAllAtOnce()
    {
        // Given
        var descriptor = $$"""
            {
              "formatVersion": 1,
              "titre": "Pack",
              "rounds": [
                { "type": "quiz", "title": "Manche", "answerSeconds": 200, "questions": [{{ValidQuestion}}] },
                { "type": "inconnu", "title": "Manche" },
                { "type": "quiz", "title": "Manche", "questions": [{ "text": 3, "choices": [{ "text": "Oui", "correct": true }] }, {{QuestionWithImage("images/absente.png")}}] }
              ]
            }
            """;

        // When
        var pack = _packs.Load(descriptor);

        // Then
        Assert.Equal(
            [
                "PackPropertyUnknown pack.json $.titre property=titre",
                "PackValueOutOfRange pack.json $.rounds[0].answerSeconds max=120 min=5",
                "PackRoundTypeUnknown pack.json $.rounds[1].type type=inconnu",
                "PackValueTypeInvalid pack.json $.rounds[2].questions[0].text expected=string",
                "PackItemCountOutOfRange pack.json $.rounds[2].questions[0].choices max=4 min=2",
                "PackPropertyMissing pack.json $ property=title",
                "PackMediaMissing pack.json $.rounds[2].questions[1].image media=images/absente.png",
            ],
            Describe(pack));
    }

    // A placeholder for a valid round, so that a descriptor shows no other problem than the one under test.
    private static string WithRound(string descriptor) =>
        descriptor.Replace("[R]", $"[{Quiz(ValidQuestion)}]", StringComparison.Ordinal);
}
