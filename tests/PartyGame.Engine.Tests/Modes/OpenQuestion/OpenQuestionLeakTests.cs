using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes.OpenQuestion;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests.Modes.OpenQuestion;

/// <summary>
/// What the views of a round of open questions may show to each viewer: the expected answer and its variants to the game
/// master only, the answer of a player to nobody but them and the game master, the question only once the game master
/// shows it, a skipped question to nobody once the round moved on, the paths of the images to nobody.
/// </summary>
public sealed class OpenQuestionLeakTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly ImmutableArray<OpenQuestionRoundDescriptor> _rounds =
        [OpenQuestionGames.Round(OpenQuestionGames.PaintingQuestion, OpenQuestionGames.FlagQuestion, OpenQuestionGames.YearQuestion)];

    private static readonly LeakSuite<GameState, OpenQuestionPhase> _suite = new()
    {
        PhaseOf = state => OpenQuestionGames.RoundOf(state).Phase,
        Project = OpenQuestionGames.Projected,
        Scenarios =
        [
            ("first question", Started()),
            ("illustrated question", OpenQuestionGames.Skipped(Started())),
            ("numeric question", OpenQuestionGames.Skipped(OpenQuestionGames.Skipped(Started()))),
            ("player joined during the presentation", OpenQuestionGames.Accepted(OpenQuestionGames.Started(_rounds, ["Zoé", "Max"]), Games.Join("Léa", player: 3))),
            ("countdown just started", OpenQuestionGames.Answering(Started())),
            ("illustrated question shown", OpenQuestionGames.Answering(OpenQuestionGames.Skipped(Started()))),
            ("some answers", OpenQuestionGames.Answering(Started(), (2, "Picasso"), (1, "Vinci"))),
            ("player joined during the answers", OpenQuestionGames.Accepted(OpenQuestionGames.Answering(OpenQuestionGames.Started(_rounds, ["Zoé", "Max"]), (1, "Vinci")), Games.Join("Léa", player: 3))),
            ("locked without answer", OpenQuestionGames.Locked(Started())),
            ("locked with answers", OpenQuestionGames.Locked(Started(), (3, "Monet"), (1, "Léonard"))),
            ("locked once everybody answered", OpenQuestionGames.Answering(Started(), (1, "Vinci"), (2, "Raphaël"), (3, "Dali"))),
            ("second question, after a skipped one", OpenQuestionGames.Skipped(OpenQuestionGames.Locked(Started(), (1, "Vinci"), (2, "Raphaël")))),
        ],
        SecretsOf = SecretsOf,
        Pairs =
        [
            // The TV screen shows a question once the game master shows it, and nothing of it before.
            new SecretPair<GameState>(
                "text of the question hidden",
                Started(),
                OpenQuestionGames.Started([_rounds[0] with { Questions = _rounds[0].Questions.SetItem(0, OpenQuestionGames.PaintingQuestion with { Text = "Qui a peint Guernica ?" }) }], _players),
                Audience.AllButGameMaster),

            ExpectedAnswerPair("expected answer, presented", state => state),
            ExpectedAnswerPair("expected answer, answers open", state => OpenQuestionGames.Answering(state, (1, "Vinci"))),
            ExpectedAnswerPair("expected answer, locked", state => OpenQuestionGames.Locked(state, (1, "Vinci"))),
            ExpectedAnswerPair("expected answer of the skipped question", state => OpenQuestionGames.Skipped(OpenQuestionGames.Locked(state, (1, "Vinci"))), Audience.Everyone),

            // The answer of a player is told to nobody but them and the game master.
            AnswerPair("answer of Zoé, answers open", OpenQuestionGames.Answering),
            AnswerPair("answer of Zoé, locked", OpenQuestionGames.Locked),
            AnswerPair("answer of Zoé on the skipped question", (state, answers) => OpenQuestionGames.Skipped(OpenQuestionGames.Locked(state, answers)), Audience.Everyone),

            // Whether a player answered shows on the TV screen as a count, never on the phones of the others.
            new SecretPair<GameState>(
                "whether Zoé answered",
                OpenQuestionGames.Answering(Started(), (2, "Picasso"), (1, "Vinci")),
                OpenQuestionGames.Answering(Started(), (2, "Picasso")),
                Audience.OtherPlayersThan("Zoé")),
            new SecretPair<GameState>(
                "whether Zoé answered, locked",
                OpenQuestionGames.Locked(Started(), (2, "Picasso"), (1, "Vinci")),
                OpenQuestionGames.Locked(Started(), (2, "Picasso")),
                Audience.OtherPlayersThan("Zoé")),
        ],
    };

    [Fact]
    public void OpenQuestionViews_EveryPhase_IsCoveredForEachRole() => _suite.AssertEveryPhaseIsCovered();

    [Fact]
    public void OpenQuestionViews_AnyScenario_ShowNoSecretToWhomItIsHiddenFrom() => _suite.AssertNoSecretIsShown();

    [Fact]
    public void OpenQuestionViews_WithoutTheSecrets_LookTheSameToWhomTheyAreHiddenFrom() => _suite.AssertPairsLookTheSame();

    private static GameState Started() => OpenQuestionGames.Started(_rounds, _players);

    /// <summary>
    /// The same game, its first question expecting one answer or another, with other variants.
    /// </summary>
    private static SecretPair<GameState> ExpectedAnswerPair(string name, Func<GameState, GameState> play, Audience? hiddenFrom = null)
    {
        var other = OpenQuestionGames.PaintingQuestion with { Answer = "Michel-Ange", AcceptedAnswers = ["Buonarroti"] };
        return new SecretPair<GameState>(
            name,
            play(Started()),
            play(OpenQuestionGames.Started([_rounds[0] with { Questions = _rounds[0].Questions.SetItem(0, other) }], _players)),
            hiddenFrom ?? Audience.AllButGameMaster);
    }

    /// <summary>
    /// The same game, where Zoé answered one thing or another, Max having answered as well. Hidden by default from all but
    /// the game master and Zoé.
    /// </summary>
    private static SecretPair<GameState> AnswerPair(string name, Func<GameState, (int, string)[], GameState> play, Audience? hiddenFrom = null) =>
        new(
            name,
            play(Started(), [(1, "Vinci"), (2, "Picasso")]),
            play(Started(), [(1, "Rembrandt"), (2, "Picasso")]),
            hiddenFrom ?? Audience.AllButGameMasterAnd("Zoé"));

    private static IEnumerable<Secret> SecretsOf(GameState state)
    {
        var round = OpenQuestionGames.RoundOf(state);

        // The question in progress shows once the game master shows it, with its image.
        if (round.Phase == OpenQuestionPhase.Presentation)
        {
            yield return new Secret(round.Question.Text, Audience.AllButGameMaster);
            if (round.Question.Image is { } image)
            {
                yield return new Secret(state.Media.UrlOf(image), Audience.AllButGameMaster);
            }
        }

        // No answer is revealed yet.
        yield return new Secret(round.Question.Answer, Audience.AllButGameMaster);
        foreach (var variant in round.Question.AcceptedAnswers)
        {
            yield return new Secret(variant, Audience.AllButGameMaster);
        }

        // The questions to come are discovered as they are played.
        foreach (var question in round.Descriptor.Questions.Skip(round.QuestionIndex + 1))
        {
            yield return new Secret(question.Text, Audience.AllButGameMaster);
            yield return new Secret(question.Answer, Audience.AllButGameMaster);
            if (question.Image is { } image)
            {
                yield return new Secret(state.Media.UrlOf(image), Audience.AllButGameMaster);
            }
        }

        // What each player typed, to nobody but them and the game master.
        foreach (var player in state.Players)
        {
            if (round.Answers.TryGetValue(player.Id, out var answer))
            {
                yield return new Secret(answer.Text, Audience.AllButGameMasterAnd(player.Nickname));
            }

            yield return new Secret(player.Nickname, Audience.OtherPlayersThan(player.Nickname));
            yield return new Secret(player.Id.Value.ToString(), Audience.OtherPlayersThan(player.Nickname));
        }

        // An image is named after what it shows, which may be the answer.
        foreach (var path in state.Media.Files.Values.Select(m => m.Value))
        {
            yield return new Secret(path, Audience.Everyone);
            foreach (var part in path.Split('/'))
            {
                yield return new Secret(Path.GetFileNameWithoutExtension(part), Audience.Everyone);
            }
        }
    }
}
