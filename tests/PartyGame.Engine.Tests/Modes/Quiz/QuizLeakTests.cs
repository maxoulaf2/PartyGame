using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// What the views of a quiz round may show to each viewer: the correct answer and the choices of the players to the game
/// master only before the reveal, the choice of a player never to the other phones, the questions as they come, a
/// skipped question to nobody once the round moved on, the paths of the images to nobody.
/// </summary>
public sealed class QuizLeakTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly ImmutableArray<QuizRoundDescriptor> _rounds =
        [QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.IllustratedQuestion, QuizGames.LastQuestion)];

    private static readonly ImmutableArray<QuizRoundDescriptor> _shuffled = [_rounds[0] with { ShuffleChoices = true }];

    private static readonly LeakSuite<GameState, QuizPhase> _suite = new()
    {
        PhaseOf = state => QuizGames.RoundOf(state).Phase,
        Project = QuizGames.Projected,
        Scenarios =
        [
            ("first question", QuizGames.Started(_rounds, _players)),
            ("shuffled choices", QuizGames.Started(_shuffled, _players)),
            ("illustrated question", QuizGames.AtQuestion(QuizGames.Started(_rounds, _players), 1)),
            ("last question", QuizGames.AtQuestion(QuizGames.Started(_rounds, _players), 2)),
            ("player joined during the presentation", QuizGames.Accepted(QuizGames.Started(_rounds, ["Zoé", "Max"]), Games.Join("Léa", player: 3))),
            ("answers just opened", QuizGames.Answering(QuizGames.Started(_rounds, _players))),
            ("some answers", QuizGames.Answering(QuizGames.Started(_shuffled, _players), (2, QuizChoiceLetter.C), (1, QuizChoiceLetter.A))),
            ("everybody answered", QuizGames.Answering(QuizGames.Started(_rounds, _players), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.A), (3, QuizChoiceLetter.D))),
            ("player joined during the answers", QuizGames.Accepted(QuizGames.Answering(QuizGames.Started(_rounds, ["Zoé", "Max"]), (1, QuizChoiceLetter.B)), Games.Join("Léa", player: 3))),
            ("locked without answer", QuizGames.Locked(QuizGames.Started(_rounds, _players))),
            ("locked with answers", QuizGames.Locked(QuizGames.Started(_rounds, _players), (3, QuizChoiceLetter.B), (1, QuizChoiceLetter.C))),
            ("locked by the timer", ByTheTimer(QuizGames.Answering(QuizGames.Started(_rounds, _players), (2, QuizChoiceLetter.A)))),
            ("revealed without answer", QuizGames.Revealed(QuizGames.Started(_rounds, _players))),
            ("revealed with answers", QuizGames.Revealed(QuizGames.Started(_shuffled, _players), (1, QuizChoiceLetter.B), (3, QuizChoiceLetter.D))),
            ("revealed to a player who joined during the answers", RevealedAfterLateJoin()),
            ("second question, after a revealed one", Next(QuizGames.Revealed(QuizGames.Started(_shuffled, _players), (1, QuizChoiceLetter.A)))),
            ("second question, after a skipped one", Skip(QuizGames.Locked(QuizGames.Started(_rounds, _players), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.B)))),
            ("second question answered, after a skipped one", QuizGames.Answering(Skip(QuizGames.Answering(QuizGames.Started(_rounds, _players), (1, QuizChoiceLetter.A))), (2, QuizChoiceLetter.B))),
        ],
        SecretsOf = SecretsOf,
        Pairs =
        [
            CorrectAnswerPair("correct answer", _rounds),
            CorrectAnswerPair("correct answer, shuffled", _shuffled),
            CorrectAnswerPair("correct answer, answers open", _rounds, state => QuizGames.Answering(state, (1, QuizChoiceLetter.A))),
            CorrectAnswerPair("correct answer, locked", _shuffled, state => QuizGames.Locked(state, (1, QuizChoiceLetter.A))),

            // The choice of a player is told to nobody but them and the game master before the reveal.
            ChoicePair("choice of Zoé, answers open", QuizGames.Answering, QuizChoiceLetter.A, QuizChoiceLetter.C),
            ChoicePair("choice of Zoé, locked", QuizGames.Locked, QuizChoiceLetter.D, QuizChoiceLetter.B),

            // Once revealed, the TV screen shows who chose what, but a phone still tells its own player only: a wrong
            // answer looks the same to the others whichever it was.
            ChoicePair("wrong choice of Zoé, revealed", QuizGames.Revealed, QuizChoiceLetter.D, QuizChoiceLetter.B, Audience.OtherPlayersThan("Zoé")),
            ChoicePair("right or wrong choice of Zoé, revealed", QuizGames.Revealed, QuizChoiceLetter.A, QuizChoiceLetter.C, Audience.OtherPlayersThan("Zoé")),

            // Whether a player answered shows on the TV screen as a count, never on the phones of the others.
            new SecretPair<GameState>(
                "whether Zoé answered",
                QuizGames.Answering(QuizGames.Started(_rounds, _players), (2, QuizChoiceLetter.B), (1, QuizChoiceLetter.A)),
                QuizGames.Answering(QuizGames.Started(_rounds, _players), (2, QuizChoiceLetter.B)),
                Audience.OtherPlayersThan("Zoé")),
            new SecretPair<GameState>(
                "whether Zoé answered, revealed",
                QuizGames.Revealed(QuizGames.Started(_rounds, _players), (2, QuizChoiceLetter.B), (1, QuizChoiceLetter.A)),
                QuizGames.Revealed(QuizGames.Started(_rounds, _players), (2, QuizChoiceLetter.B)),
                Audience.OtherPlayersThan("Zoé")),

            // A skipped question leaves no trace: neither its correct answer nor what the players chose.
            CorrectAnswerPair("correct answer of the skipped question", _rounds, state => Skip(QuizGames.Locked(state, (1, QuizChoiceLetter.A))), Audience.Everyone),
            ChoicePair("choice of Zoé on the skipped question", (state, answers) => Skip(QuizGames.Locked(state, answers)), QuizChoiceLetter.A, QuizChoiceLetter.C, Audience.Everyone),
            new SecretPair<GameState>(
                "whether Zoé answered the skipped question",
                Skip(QuizGames.Answering(QuizGames.Started(_rounds, _players), (1, QuizChoiceLetter.A))),
                Skip(QuizGames.Answering(QuizGames.Started(_rounds, _players))),
                Audience.Everyone),
        ],
    };

    [Fact]
    public void QuizViews_EveryPhase_IsCoveredForEachRole() => _suite.AssertEveryPhaseIsCovered();

    [Fact]
    public void QuizViews_AnyScenario_ShowNoSecretToWhomItIsHiddenFrom() => _suite.AssertNoSecretIsShown();

    [Fact]
    public void QuizViews_WithoutTheSecrets_LookTheSameToWhomTheyAreHiddenFrom() => _suite.AssertPairsLookTheSame();

    /// <summary>
    /// The same game, its first question with the correct answer first, as authors often write it, or third: the shuffle,
    /// drawn from the same seed, puts the choices in the same order.
    /// </summary>
    private static SecretPair<GameState> CorrectAnswerPair(
        string name,
        ImmutableArray<QuizRoundDescriptor> rounds,
        Func<GameState, GameState>? play = null,
        Audience? hiddenFrom = null)
    {
        play ??= state => state;
        var moved = rounds[0] with { Questions = rounds[0].Questions.SetItem(0, QuizGames.WithCorrectChoice(rounds[0].Questions[0], 2)) };
        return new SecretPair<GameState>(
            name,
            play(QuizGames.Started(rounds, _players)),
            play(QuizGames.Started([moved], _players)),
            hiddenFrom ?? Audience.AllButGameMaster);
    }

    /// <summary>
    /// The same game, where Zoé chose one choice or another, Max having answered as well. Hidden by default from all but
    /// the game master and Zoé, as before the reveal.
    /// </summary>
    private static SecretPair<GameState> ChoicePair(
        string name,
        Func<GameState, (int, QuizChoiceLetter)[], GameState> play,
        QuizChoiceLetter one,
        QuizChoiceLetter other,
        Audience? hiddenFrom = null) =>
        new(
            name,
            play(QuizGames.Started(_rounds, _players), [(1, one), (2, QuizChoiceLetter.B)]),
            play(QuizGames.Started(_rounds, _players), [(1, other), (2, QuizChoiceLetter.B)]),
            hiddenFrom ?? Audience.AllButGameMasterAnd("Zoé"));

    /// <summary>
    /// A game revealed with Léa, who joined once the answers were open: she sees the correct answer without verdict.
    /// </summary>
    private static GameState RevealedAfterLateJoin()
    {
        var state = QuizGames.Accepted(QuizGames.Answering(QuizGames.Started(_rounds, ["Zoé", "Max"]), (1, QuizChoiceLetter.B)), Games.Join("Léa", player: 3));
        state = QuizGames.Accepted(state, QuizGames.LockAnswers(state));
        return QuizGames.Accepted(state, QuizGames.RevealAnswer(state));
    }

    /// <summary>
    /// The same game, moved on from its revealed question to the next one.
    /// </summary>
    private static GameState Next(GameState state) => QuizGames.Accepted(state, QuizGames.NextQuestion(state));

    /// <summary>
    /// The same game, its question in progress skipped for the next one.
    /// </summary>
    private static GameState Skip(GameState state) => QuizGames.Accepted(state, QuizGames.SkipQuestion(state));

    /// <summary>
    /// The same game, its answers locked by their timer rather than by the game master.
    /// </summary>
    private static GameState ByTheTimer(GameState state) => QuizGames.Accepted(state, QuizGames.AnswersTimerElapsed(state));

    private static IEnumerable<Secret> SecretsOf(GameState state)
    {
        var round = QuizGames.RoundOf(state);

        // The questions to come, and their images, are discovered as they are played.
        foreach (var question in round.Descriptor.Questions.Skip(round.QuestionIndex + 1))
        {
            yield return new Secret(question.Text, Audience.AllButGameMaster);
            foreach (var choice in question.Choices)
            {
                yield return new Secret(choice.Text, Audience.AllButGameMaster);
            }

            if (question.Image is { } image)
            {
                yield return new Secret(state.Media.UrlOf(image), Audience.AllButGameMaster);
            }
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

        foreach (var player in state.Players)
        {
            yield return new Secret(player.Nickname, Audience.OtherPlayersThan(player.Nickname));
            yield return new Secret(player.Id.Value.ToString(), Audience.OtherPlayersThan(player.Nickname));
        }
    }
}
