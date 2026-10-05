using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.OpenQuestion;
using static PartyGame.Content.Tests.TestPacks;

namespace PartyGame.Content.Tests;

/// <summary>
/// The checks of an open question round, as the loading of a pack reports them to the game master.
/// </summary>
public sealed class OpenQuestionProblemsTests : IDisposable
{
    private readonly TestPacks _packs = new();

    public void Dispose() => _packs.Dispose();

    [Fact]
    public void Load_ValidOpenQuestionRound_IsValid()
    {
        // Given
        var folder = _packs.Add("pack", Pack("""
            { "type": "openquestion", "title": "Manche", "questions": [
                { "text": "Qui a peint La Joconde ?", "answer": "Léonard de Vinci", "acceptedAnswers": ["Vinci"], "image": "images/joconde.jpg" },
                { "text": "Année de la prise de la Bastille ?", "answer": "1789", "inputMode": "numeric" }
            ] }
            """), "images/joconde.jpg");

        // When
        var pack = Load(folder);

        // Then
        Assert.True(pack.IsValid, string.Join(Environment.NewLine, Describe(pack)));
    }

    [Fact]
    public void Load_OpenQuestionRoundWithInvalidQuestions_ReportsEachProblemAtItsPathInTheDescriptor()
    {
        // Given: a missing image, a variant too long, a numeric answer with letters, an answer that normalizes to nothing
        // and a variant that repeats another, then, in a round of its own since it cannot be read, an unknown keyboard
        var folder = _packs.Add("pack", Pack("""
            { "type": "openquestion", "title": "Manche", "maxLength": 10, "questions": [
                { "text": "Question ?", "answer": "Paris", "image": "images/absente.png" },
                { "text": "Question ?", "answer": "Paris", "acceptedAnswers": ["Paris, la capitale"] },
                { "text": "Question ?", "answer": "douze", "inputMode": "numeric" },
                { "text": "Question ?", "answer": "Les", "acceptedAnswers": ["PARIS", "paris"] }
            ] }
            """, """
            { "type": "openquestion", "title": "Manche", "questions": [{ "text": "Question ?", "answer": "Paris", "inputMode": "voice" }] }
            """));

        // When
        var pack = Load(folder);

        // Then
        Assert.False(pack.IsValid);
        Assert.Equal(
            [
                "OpenQuestionAnswerDuplicated pack.json $.rounds[0].questions[3].acceptedAnswers[1] answer=paris",
                "OpenQuestionAnswerEmpty pack.json $.rounds[0].questions[3].answer answer=Les",
                "OpenQuestionAnswerLengthOutOfRange pack.json $.rounds[0].questions[1].acceptedAnswers[0] max=10",
                "OpenQuestionAnswerNotNumeric pack.json $.rounds[0].questions[2].answer answer=douze",
                "PackMediaMissing pack.json $.rounds[0].questions[0].image media=images/absente.png",
                "PackValueTypeInvalid pack.json $.rounds[1].questions[0].inputMode expected=string",
            ],
            Describe(pack).Order(StringComparer.Ordinal));
    }

    private static LoadedPack Load(string folder) => new PackLoader(new GameModes([new OpenQuestionMode()]).Validate).Load(folder);
}
