using System.Collections.Immutable;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.OpenQuestion;

namespace PartyGame.Engine.Tests.Modes.OpenQuestion;

/// <summary>
/// The reveal of an open question once judged: the points it awards, what each screen shows of it, and how the game
/// master moves on.
/// </summary>
public sealed class OpenQuestionRevealTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa", "Tom"];

    private static readonly OpenQuestionRoundDescriptor _round =
        OpenQuestionGames.Round(OpenQuestionGames.PaintingQuestion, OpenQuestionGames.YearQuestion);

    private static readonly OpenQuestionRoundDescriptor _fast = _round with { Points = 1000, SpeedBonus = 500, AnswerSeconds = 20 };

    [Fact]
    public void Handle_RevealAnswerWithoutSpeedBonus_AwardsThePointsOfTheRoundToTheAcceptedAnswersOnly()
    {
        // Given: Zoé and Léa accepted, Max refused, Tom without answer
        var judged = OpenQuestionGames.Judged(Presented(_round), [1, 3], (1, "Vinci"), (2, "Picasso"), (3, "De Vinci"));

        // When
        var transition = OpenQuestionGames.Engine.Handle(judged, OpenQuestionGames.RevealAnswer(judged), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var revealed = transition.State;
        Assert.Equal(OpenQuestionPhase.Revealed, OpenQuestionGames.RoundOf(revealed).Phase);
        Assert.Equal([1000, 0, 1000, 0], revealed.Players.Select(p => p.Score));
        var points = OpenQuestionGames.RoundOf(revealed).Points;
        Assert.Equal([1000, 0, 1000, 0], revealed.Players.Select(p => points[p.Id]));
    }

    [Theory]
    [InlineData(0, 1500)]
    [InlineData(5_000, 1375)]
    [InlineData(10_000, 1250)]
    [InlineData(19_999, 1000)]
    public void Handle_RevealAnswerWithSpeedBonus_AddsTheBonusInProportionToTheTimeLeft(int receivedAfterMilliseconds, int expected)
    {
        var revealed = AcceptedAfter(_fast, receivedAfterMilliseconds);

        Assert.Equal(expected, revealed.Players[0].Score);
    }

    [Theory]
    [InlineData(1_000, 316)] // 333 × 19/20 = 316.35
    [InlineData(10_000, 167)] // 333 × 1/2 = 166.5, rounded up
    public void Handle_RevealAnswerWithSpeedBonus_RoundsTheBonusToTheNearestInteger(int receivedAfterMilliseconds, int expected)
    {
        var revealed = AcceptedAfter(_fast with { Points = 0, SpeedBonus = 333 }, receivedAfterMilliseconds);

        Assert.Equal(expected, revealed.Players[0].Score);
    }

    [Fact]
    public void Handle_RevealAnswerWithSpeedBonus_GivesNothingToARefusedAnswer()
    {
        var revealed = OpenQuestionGames.Revealed(Presented(_fast), [], (1, "Vinci"));

        Assert.Equal(0, revealed.Players[0].Score);
    }

    [Fact]
    public void Handle_RevealAnswer_WithoutAnyAnswer_AwardsNothing()
    {
        var revealed = OpenQuestionGames.Revealed(Presented(_fast), []);

        Assert.Equal(OpenQuestionPhase.Revealed, OpenQuestionGames.RoundOf(revealed).Phase);
        Assert.All(revealed.Players, player => Assert.Equal(0, player.Score));
    }

    [Fact]
    public void Handle_RevealAnswer_Twice_IsRejected_AndCountsThePointsOnce()
    {
        var revealed = OpenQuestionGames.Revealed(Presented(_round), [1], (1, "Vinci"));

        AssertRejected(revealed, s => OpenQuestionGames.RevealAnswer(s), RejectionReason.PhaseMismatch);
        Assert.Equal(1000, revealed.Players[0].Score);
    }

    [Fact]
    public void Handle_RevealAnswer_BeforeTheJudgment_IsRejected() =>
        AssertRejected(OpenQuestionGames.Locked(Presented(_round), (1, "Vinci")), s => OpenQuestionGames.RevealAnswer(s), RejectionReason.PhaseMismatch);

    [Fact]
    public void Handle_RevealAnswer_OtherQuestion_IsRejected() =>
        AssertRejected(OpenQuestionGames.Judged(Presented(_round), [1], (1, "Vinci")), s => OpenQuestionGames.RevealAnswer(s, questionNumber: 2), RejectionReason.QuestionMismatch);

    [Fact]
    public void Handle_SkipQuestion_Revealed_IsRejected() =>
        AssertRejected(OpenQuestionGames.Revealed(Presented(_round), [1], (1, "Vinci")), s => OpenQuestionGames.SkipQuestion(s), RejectionReason.PhaseMismatch);

    [Fact]
    public void Handle_NextQuestion_Revealed_PresentsTheNextOne_KeepingTheScores()
    {
        var state = OpenQuestionGames.Revealed(Presented(_round), [1], (1, "Vinci"));

        var next = OpenQuestionGames.Accepted(state, OpenQuestionGames.NextQuestion(state));

        var round = OpenQuestionGames.RoundOf(next);
        Assert.Equal((1, OpenQuestionPhase.Presentation), (round.QuestionIndex, round.Phase));
        Assert.Empty(round.Points);
        Assert.Empty(round.Answers);
        Assert.Equal(1000, next.Players[0].Score);
    }

    [Fact]
    public void Handle_NextQuestion_LastQuestionOfTheLastRoundRevealed_FinishesTheGame()
    {
        var state = OpenQuestionGames.Revealed(OpenQuestionGames.Skipped(Presented(_round)), [], (1, "1968"));

        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.NextQuestion(state), Games.Context());

        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase);
    }

    [Fact]
    public void Handle_NextQuestion_BeforeTheReveal_IsRejected() =>
        AssertRejected(OpenQuestionGames.Judged(Presented(_round), [1], (1, "Vinci")), s => OpenQuestionGames.NextQuestion(s), RejectionReason.PhaseMismatch);

    [Fact]
    public void Handle_NextQuestion_Twice_IsRejected()
    {
        var state = OpenQuestionGames.Revealed(Presented(_round), [1], (1, "Vinci"));
        var input = OpenQuestionGames.NextQuestion(state);

        AssertRejected(OpenQuestionGames.Accepted(state, input), _ => input, RejectionReason.QuestionMismatch);
    }

    [Fact]
    public void ProjectForDisplay_Revealed_ShowsTheExpectedAnswer_TheCorrectGroupsFirst_ThenThePlayersWithoutAnswer()
    {
        // Given: Max and Léa typed the same refused answer, Zoé a correct one, Tom nothing
        var state = OpenQuestionGames.Revealed(Presented(_round), [1], (2, "Picasso"), (1, "de vinci"), (3, "picasso"));

        // When
        var view = Assert.IsType<OpenQuestionDisplayView>(OpenQuestionGames.Snapshots.ForDisplay(state).RoundView);

        // Then
        var reveal = Assert.IsType<OpenQuestionDisplayReveal>(view.Reveal);
        Assert.Equal("Léonard de Vinci", reveal.ExpectedAnswer);
        Assert.Equal(
            [("de vinci", true, "Zoé"), ("Picasso", false, "Max, Léa")],
            reveal.Groups.Select(group => (group.Text, group.Correct, string.Join(", ", group.Nicknames))));
        Assert.Equal(["Tom"], reveal.WithoutAnswer);
    }

    [Fact]
    public void ProjectForDisplay_GroupJudgedPartly_SplitsItsAuthors_AsThePointsDo()
    {
        var state = OpenQuestionGames.Revealed(Presented(_round), [2], (1, "Vinci"), (2, "vinci"));

        var reveal = Assert.IsType<OpenQuestionDisplayView>(OpenQuestionGames.Snapshots.ForDisplay(state).RoundView).Reveal!;

        Assert.Equal(
            [("Vinci", true, "Max"), ("Vinci", false, "Zoé")],
            reveal.Groups.Select(group => (group.Text, group.Correct, string.Join(", ", group.Nicknames))));
    }

    [Fact]
    public void ProjectForPlayer_Revealed_ShowsTheVerdictTheExpectedAnswerAndThePointsOfThePlayer()
    {
        var state = OpenQuestionGames.Revealed(Presented(_round), [1], (1, "Vinci"), (2, "Picasso"));

        var views = state.Players.Select(p => Assert.IsType<OpenQuestionPlayerView>(OpenQuestionGames.Snapshots.ForPlayer(state, p).RoundView)).ToArray();

        Assert.Equal(
            [
                (OpenQuestionVerdict.Correct, 1000),
                (OpenQuestionVerdict.Wrong, 0),
                (OpenQuestionVerdict.NoAnswer, 0),
                (OpenQuestionVerdict.NoAnswer, 0),
            ],
            views.Select(view => (view.Verdict!.Value, view.Points!.Value)));
        Assert.All(views, view => Assert.Equal("Léonard de Vinci", view.ExpectedAnswer));
    }

    [Fact]
    public void ProjectForPlayer_PlayerJoinedAfterTheQuestionShowed_GetsNeitherVerdictNorPoints()
    {
        var state = OpenQuestionGames.Answering(Presented(_round), (1, "Vinci"));
        state = OpenQuestionGames.Accepted(state, Games.Join("Ana", player: 5));
        state = OpenQuestionGames.Accepted(state, OpenQuestionGames.AnswersTimerElapsed(state));
        state = OpenQuestionGames.Accepted(state, OpenQuestionGames.Judge(state, [1]));
        state = OpenQuestionGames.Accepted(state, OpenQuestionGames.RevealAnswer(state));

        var view = Assert.IsType<OpenQuestionPlayerView>(OpenQuestionGames.Snapshots.ForPlayer(state, state.Players[^1]).RoundView);

        Assert.Equal((null, null), (view.Verdict, view.Points));
    }

    [Fact]
    public void ProjectForGameMaster_Revealed_ShowsThePointsOfEachParticipant()
    {
        var state = OpenQuestionGames.Revealed(Presented(_round), [1], (1, "Vinci"), (2, "Picasso"));

        var view = Assert.IsType<OpenQuestionGameMasterView>(OpenQuestionGames.Snapshots.ForGameMaster(state).RoundView);

        Assert.Equal(OpenQuestionQuestionPhase.Revealed, view.Phase);
        Assert.Equal([1000, 0, 0, 0], view.Answers.Select(answer => answer.Points));
        Assert.Equal([true, false], view.Groups.Select(group => group.Accepted));
    }

    private static GameState Presented(OpenQuestionRoundDescriptor round) =>
        OpenQuestionGames.Started(ImmutableArray.Create(round), _players);

    /// <summary>
    /// The game where Zoé answered right, received by the hub the given time after the question showed, then revealed.
    /// </summary>
    private static GameState AcceptedAfter(OpenQuestionRoundDescriptor round, int receivedAfterMilliseconds)
    {
        var state = OpenQuestionGames.Answering(Presented(round));
        state = OpenQuestionGames.Accepted(state, OpenQuestionGames.Answer(state, 1, "Vinci", Games.Now.AddMilliseconds(receivedAfterMilliseconds)));
        state = OpenQuestionGames.Accepted(state, OpenQuestionGames.AnswersTimerElapsed(state));
        state = OpenQuestionGames.Accepted(state, OpenQuestionGames.Judge(state, [1]));
        return OpenQuestionGames.Accepted(state, OpenQuestionGames.RevealAnswer(state));
    }

    private static void AssertRejected(GameState state, Func<GameState, GameInput> input, RejectionReason reason)
    {
        var transition = OpenQuestionGames.Engine.Handle(state, input(state), Games.Context());

        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }
}
