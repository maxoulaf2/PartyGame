using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// What the views of a quiz round may show to each viewer: the correct answer to the game master only before its reveal,
/// the questions as they come, the paths of the images to nobody.
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
        ],
        SecretsOf = SecretsOf,
        Pairs =
        [
            CorrectAnswerPair("correct answer", _rounds),
            CorrectAnswerPair("correct answer, shuffled", _shuffled),
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
    private static SecretPair<GameState> CorrectAnswerPair(string name, ImmutableArray<QuizRoundDescriptor> rounds)
    {
        var moved = rounds[0] with { Questions = rounds[0].Questions.SetItem(0, QuizGames.WithCorrectChoice(rounds[0].Questions[0], 2)) };
        return new SecretPair<GameState>(name, QuizGames.Started(rounds, _players), QuizGames.Started([moved], _players), Audience.AllButGameMaster);
    }

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
