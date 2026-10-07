using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes.QuizBuzzer;
using PartyGame.Engine.State;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests.Modes.QuizBuzzer;

/// <summary>
/// What the views of a round of buzzer questions may show to each viewer: the expected answer to the game master only, a
/// question once asked, the questions to come to nobody else, who buzzed during the arbitration to nobody but the player
/// themselves, who was refused to nobody but the player themselves, the time stamps of the buzzes to nobody, the paths of
/// the images to nobody.
/// </summary>
public sealed class BuzzerLeakTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly BuzzerRoundDescriptor _round =
        BuzzerGames.Round(BuzzerGames.PaintingQuestion, BuzzerGames.IllustratedQuestion, BuzzerGames.LastQuestion);

    private static readonly LeakSuite<GameState, BuzzerPhase> _suite = new()
    {
        PhaseOf = state => BuzzerGames.RoundOf(state).Phase,
        Project = BuzzerGames.Projected,
        Scenarios =
        [
            ("first question announced", Started()),
            ("illustrated question announced", BuzzerGames.AtQuestion(Started(), 1)),
            ("last question announced", BuzzerGames.AtQuestion(Started(), 2)),
            ("question asked", BuzzerGames.Asked(Started())),
            ("illustrated question asked", BuzzerGames.Asked(BuzzerGames.AtQuestion(Started(), 1))),
            ("player joined once asked", BuzzerGames.Accepted(BuzzerGames.Asked(BuzzerGames.Started(_round, "Zoé", "Max")), Games.Join("Léa", player: 3))),
            ("one buzz, arbitrating", BuzzerGames.Buzzed(Started(), (2, 40))),
            ("two buzzes, arbitrating", BuzzerGames.Buzzed(Started(), (2, 40), (1, 10))),
            ("blocked player, arbitrating", BuzzerGames.WithBlocked(BuzzerGames.Buzzed(Started(), (2, 40)), 3)),
            ("winner designated", BuzzerGames.Answering(Started(), (2, 40), (1, 10))),
            ("illustrated question, winner designated", BuzzerGames.Answering(BuzzerGames.AtQuestion(Started(), 1), (3, 0))),
            ("blocked player, winner designated", BuzzerGames.WithBlocked(BuzzerGames.Answering(Started(), (2, 40)), 1)),
            ("wrong answer, buzzer reopened", BuzzerGames.Judged(Started(), correct: false, 1)),
            ("every player blocked", BuzzerGames.Judged(Started(), correct: false, 1, 2, 3)),
            ("found after a wrong answer", BuzzerGames.Judged(Started(), correct: true, 1, 2)),
            ("revealed during the arbitration", Revealed(BuzzerGames.Buzzed(Started(), (2, 40)))),
            ("illustrated question revealed", Revealed(BuzzerGames.Asked(BuzzerGames.AtQuestion(Started(), 1)))),
            ("question asked hidden", BuzzerGames.AskedHidden(Started())),
            ("illustrated question asked hidden", BuzzerGames.AskedHidden(BuzzerGames.AtQuestion(Started(), 1))),
            ("question hidden, arbitrating", BuzzerGames.Hidden(BuzzerGames.Buzzed(Started(), (2, 40)))),
            ("question hidden, winner designated", BuzzerGames.Hidden(BuzzerGames.Answering(Started(), (2, 40)))),
            ("question hidden, every player blocked", BuzzerGames.Hidden(BuzzerGames.Judged(Started(), correct: false, 1, 2, 3))),
            ("last question found", BuzzerGames.Judged(BuzzerGames.AtQuestion(Started(), 2), correct: true, 3)),
        ],
        SecretsOf = SecretsOf,
        Pairs =
        [
            AnswerPair("expected answer, announced", state => state),
            AnswerPair("expected answer, asked", BuzzerGames.Asked),
            AnswerPair("expected answer, arbitrating", state => BuzzerGames.Buzzed(state, (1, 10))),
            AnswerPair("expected answer, winner designated", state => BuzzerGames.Answering(state, (1, 10))),
            AnswerPair("expected answer, every player blocked", state => BuzzerGames.Judged(state, correct: false, 1, 2, 3)),

            // The TV screen shows a question once the game master asks it, and nothing of it before.
            new SecretPair<GameState>(
                "text of the question, announced",
                Started(),
                BuzzerGames.Started(WithFirstQuestion(BuzzerGames.PaintingQuestion with { Text = "Qui a sculpté Le Penseur ?" }), _players),
                Audience.AllButGameMaster),
            new SecretPair<GameState>(
                "text of the question, asked hidden",
                BuzzerGames.AskedHidden(Started()),
                BuzzerGames.AskedHidden(BuzzerGames.Started(WithFirstQuestion(BuzzerGames.PaintingQuestion with { Text = "Qui a sculpté Le Penseur ?" }), _players)),
                Audience.AllButGameMaster),

            // Nobody learns who buzzed before the winner is designated, but the player who did.
            new SecretPair<GameState>(
                "whether Max buzzed too, arbitrating",
                BuzzerGames.Buzzed(Started(), (1, 10), (2, 20)),
                BuzzerGames.Buzzed(Started(), (1, 10)),
                Audience.AllButGameMasterAnd("Max")),

            // The time stamps decide, and stay on the server: the same winner looks the same however fast they were.
            new SecretPair<GameState>(
                "when Zoé pressed, winner designated",
                BuzzerGames.Answering(Started(), (2, 200), (1, 10)),
                BuzzerGames.Answering(Started(), (2, 200), (1, 150)),
                Audience.Everyone),
            new SecretPair<GameState>(
                "when Max pressed after Zoé, winner designated",
                BuzzerGames.Answering(Started(), (1, 10), (2, 20)),
                BuzzerGames.Answering(Started(), (1, 10), (2, 200)),
                Audience.AllButGameMasterAnd("Max")),
        ],
    };

    [Fact]
    public void BuzzerViews_EveryPhase_IsCoveredForEachRole() => _suite.AssertEveryPhaseIsCovered();

    [Fact]
    public void BuzzerViews_AnyScenario_ShowNoSecretToWhomItIsHiddenFrom() => _suite.AssertNoSecretIsShown();

    [Fact]
    public void BuzzerViews_WithoutTheSecrets_LookTheSameToWhomTheyAreHiddenFrom() => _suite.AssertPairsLookTheSame();

    private static GameState Started() => BuzzerGames.Started(_round, _players);

    private static GameState Revealed(GameState state) => BuzzerGames.Accepted(state, BuzzerGames.RevealAnswer(state));

    private static BuzzerRoundDescriptor WithFirstQuestion(BuzzerQuestion question) =>
        _round with { Questions = _round.Questions.SetItem(0, question) };

    /// <summary>
    /// The same game, the expected answer of its first question worded otherwise.
    /// </summary>
    private static SecretPair<GameState> AnswerPair(string name, Func<GameState, GameState> play) =>
        new(
            name,
            play(Started()),
            play(BuzzerGames.Started(WithFirstQuestion(BuzzerGames.PaintingQuestion with { Answer = "Michel-Ange" }), _players)),
            Audience.AllButGameMaster);

    private static IEnumerable<Secret> SecretsOf(GameState state)
    {
        var round = BuzzerGames.RoundOf(state);

        // The expected answer shows to everybody at the reveal only.
        if (round.Phase != BuzzerPhase.Revealed)
        {
            yield return new Secret(round.Question.Answer, Audience.AllButGameMaster);
        }

        // The question shows on the TV screen once the game master shows it, at the latest at the reveal.
        if (!round.Shown && !round.Revealed)
        {
            yield return new Secret(round.Question.Text, Audience.AllButGameMaster);
            if (round.Question.Image is { } image)
            {
                yield return new Secret(state.Media.UrlOf(image), Audience.AllButGameMaster);
            }
        }

        // The questions to come, and their images, are discovered as they are played.
        foreach (var question in round.Descriptor.Questions.Skip(round.QuestionIndex + 1))
        {
            yield return new Secret(question.Text, Audience.AllButGameMaster);
            yield return new Secret(question.Answer, Audience.AllButGameMaster);
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

        // The winner, then who found, is named on every screen, the other players on their phone alone: never who was
        // refused.
        foreach (var player in state.Players)
        {
            if (player.Id != round.Buzzer.Winner && player.Id != round.FoundBy)
            {
                yield return new Secret(player.Nickname, Audience.OtherPlayersThan(player.Nickname));
            }

            yield return new Secret(player.Id.Value.ToString(), Audience.OtherPlayersThan(player.Nickname));
        }
    }
}
