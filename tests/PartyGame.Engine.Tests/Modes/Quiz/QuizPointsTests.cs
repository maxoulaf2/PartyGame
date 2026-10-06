using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Modes.Quiz;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// The points of a quiz question: awarded at its reveal to the correct answers, with the speed bonus of the round in
/// proportion to the time left, and added to the scores of the game.
/// </summary>
public sealed class QuizPointsTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly QuizRoundDescriptor _round = QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.LastQuestion);

    /// <summary>A round with a speed bonus, whose questions are answered in 20 seconds.</summary>
    private static readonly QuizRoundDescriptor _fast = _round with { Points = 1000, SpeedBonus = 500, AnswerSeconds = 20 };

    [Fact]
    public void Handle_RevealAnswerWithoutSpeedBonus_AwardsThePointsOfTheRoundToTheCorrectAnswersOnly()
    {
        // Given: Zoé is right, Max is wrong, Léa does not answer
        var state = QuizGames.Locked(Presented(_round), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.D));

        // When
        var revealed = QuizGames.Accepted(state, QuizGames.RevealAnswer(state));

        // Then
        Assert.Equal([QuizRoundDescriptor.DefaultPoints, 0, 0], revealed.Players.Select(p => p.Score));
        var points = QuizGames.RoundOf(revealed).Points;
        Assert.Equal(3, points.Count);
        Assert.Equal([QuizRoundDescriptor.DefaultPoints, 0, 0], revealed.Players.Select(p => points[p.Id]));
    }

    [Fact]
    public void Handle_RevealAnswerWithoutSpeedBonus_AwardsTheSamePointsHoweverLateTheAnswer()
    {
        // Given
        var state = Presented(_round);
        state = QuizGames.Shown(state);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A, Games.Now.AddSeconds(1)));
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.A, Games.Now.AddSeconds(19)));

        // When
        var revealed = Reveal(state);

        // Then
        Assert.Equal([1000, 1000, 0], revealed.Players.Select(p => p.Score));
    }

    [Theory]
    [InlineData(0, 1500)] // received as the answers open: the whole bonus
    [InlineData(5_000, 1375)]
    [InlineData(10_000, 1250)]
    [InlineData(19_999, 1000)] // a millisecond before the end: nothing left of the bonus once rounded
    public void Handle_RevealAnswerWithSpeedBonus_AddsTheBonusInProportionToTheTimeLeft(int receivedAfterMilliseconds, int expected)
    {
        // Given
        var state = AnsweredAfter(_fast, receivedAfterMilliseconds);

        // When
        var revealed = Reveal(state);

        // Then
        Assert.Equal(expected, revealed.Players[0].Score);
    }

    [Theory]
    [InlineData(10_000, 1167)] // 333 × 10 / 20 = 166.5, rounded up
    [InlineData(13_000, 1117)] // 333 × 7 / 20 = 116.55
    [InlineData(17_000, 1050)] // 333 × 3 / 20 = 49.95
    [InlineData(19_000, 1017)] // 333 × 1 / 20 = 16.65
    public void Handle_RevealAnswerWithSpeedBonus_RoundsTheBonusToTheNearestInteger(int receivedAfterMilliseconds, int expected)
    {
        // Given
        var state = AnsweredAfter(_fast with { SpeedBonus = 333 }, receivedAfterMilliseconds);

        // When
        var revealed = Reveal(state);

        // Then
        Assert.Equal(expected, revealed.Players[0].Score);
    }

    [Fact]
    public void Handle_RevealAnswerWithSpeedBonus_MeasuresTheTimeLeftAgainstTheDurationOfTheQuestion()
    {
        // Given: the question is answered in 10 seconds rather than the 20 of the round, and Zoé answers after 5
        var round = _fast with { Questions = _fast.Questions.SetItem(0, QuizGames.CapitalQuestion with { AnswerSeconds = 10 }) };
        var state = AnsweredAfter(round, 5_000);

        // When
        var revealed = Reveal(state);

        // Then: half of the bonus, not three quarters
        Assert.Equal(1250, revealed.Players[0].Score);
    }

    [Fact]
    public void Handle_RevealAnswerLockedEarly_MeasuresTheTimeLeftAgainstTheEndOfTheCountdown()
    {
        // Given: Zoé answers after 5 seconds, then Max and Léa right after, which locks the answers at once
        var state = AnsweredAfter(_fast, 5_000);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.B, Games.Now.AddMilliseconds(5_500)));
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 3, QuizChoiceLetter.C, Games.Now.AddMilliseconds(6_000)));
        Assert.Equal(QuizPhase.Locked, QuizGames.RoundOf(state).Phase);

        // When
        var revealed = Reveal(state);

        // Then: what was left of the countdown when the answer arrived, not when it was locked
        Assert.Equal(1375, revealed.Players[0].Score);
    }

    [Fact]
    public void Handle_RevealAnswerStampedBeforeTheCountdown_GetsTheWholeBonusOnly()
    {
        // Given: the hub stamped the answer just before the loop handled the last choice shown
        var state = AnsweredAfter(_fast, -100);

        // When
        var revealed = Reveal(state);

        // Then
        Assert.Equal(1500, revealed.Players[0].Score);
    }

    [Fact]
    public void Handle_RevealAnswerGivenWhileTheChoicesShowed_GetsTheWholeBonus()
    {
        // Given: Zoé answered right as soon as the correct choice showed, long before the last one
        var state = QuizGames.Shown(Presented(_fast), choiceCount: 1);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A, Games.Now.AddSeconds(-30)));
        state = QuizGames.Shown(state);

        // When
        var revealed = Reveal(state);

        // Then
        Assert.Equal(1500, revealed.Players[0].Score);
    }

    [Fact]
    public void Handle_RevealAnswerLockedWithoutCountdown_GivesTheWholeBonusToTheRightAnswers()
    {
        // Given: everybody answered before the last choice showed, which locked the answers without countdown
        var state = QuizGames.Shown(Presented(_fast), choiceCount: 3);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A));
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.B));
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 3, QuizChoiceLetter.A));
        state = QuizGames.Shown(state);
        Assert.Equal((QuizPhase.Locked, null), (QuizGames.RoundOf(state).Phase, QuizGames.RoundOf(state).AnswersCloseAt));

        // When
        var revealed = Reveal(state);

        // Then
        Assert.Equal([1500, 0, 1500], revealed.Players.Select(p => p.Score));
    }

    [Fact]
    public void Handle_RevealAnswerWithSpeedBonus_GivesNothingToAWrongAnswer()
    {
        // Given
        var state = QuizGames.Locked(Presented(_fast), (1, QuizChoiceLetter.B));

        // When
        var revealed = QuizGames.Accepted(state, QuizGames.RevealAnswer(state));

        // Then
        Assert.Equal(0, revealed.Players[0].Score);
    }

    [Theory]
    [InlineData(QuizPhase.Presentation)]
    [InlineData(QuizPhase.Answering)]
    [InlineData(QuizPhase.Locked)]
    public void Handle_BeforeTheReveal_AwardsNoPoint(QuizPhase phase)
    {
        // Given / When: everybody answers right, but the last one while the answers are still open
        var presented = Presented(_fast);
        var state = phase switch
        {
            QuizPhase.Presentation => presented,
            QuizPhase.Answering => QuizGames.Answering(presented, (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.A)),
            _ => QuizGames.Locked(presented, (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.A), (3, QuizChoiceLetter.A)),
        };

        // Then
        Assert.All(state.Players, player => Assert.Equal(0, player.Score));
        Assert.Empty(QuizGames.RoundOf(state).Points);
    }

    [Theory]
    [InlineData(QuizPhase.Presentation)]
    [InlineData(QuizPhase.Answering)]
    [InlineData(QuizPhase.Locked)]
    public void Handle_SkipQuestionBeforeItsReveal_AwardsNoPoint(QuizPhase phase)
    {
        // Given
        var presented = Presented(_fast);
        var state = phase switch
        {
            QuizPhase.Presentation => presented,
            QuizPhase.Answering => QuizGames.Answering(presented, (1, QuizChoiceLetter.A)),
            _ => QuizGames.Locked(presented, (1, QuizChoiceLetter.A)),
        };

        // When
        var skipped = QuizGames.Accepted(state, QuizGames.SkipQuestion(state));

        // Then: the next question comes, and nobody scored for the skipped one
        Assert.Equal(2, QuizGames.RoundOf(skipped).QuestionNumber);
        Assert.All(skipped.Players, player => Assert.Equal(0, player.Score));
        Assert.Empty(QuizGames.RoundOf(skipped).Points);
    }

    [Fact]
    public void Handle_NextQuestion_KeepsTheScoresAndForgetsThePointsOfTheQuestion()
    {
        // Given
        var state = QuizGames.Revealed(Presented(_round), (1, QuizChoiceLetter.A));

        // When
        var next = QuizGames.Accepted(state, QuizGames.NextQuestion(state));

        // Then
        Assert.Equal([1000, 0, 0], next.Players.Select(p => p.Score));
        Assert.Empty(QuizGames.RoundOf(next).Points);
        var view = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(next, next.Players[0]).RoundView!;
        Assert.Null(view.Points);
    }

    [Fact]
    public void Handle_PointsOfSeveralQuestionsAndRounds_AddUp()
    {
        // Given: two rounds, the second one worth more
        var second = QuizGames.Round(QuizGames.IllustratedQuestion) with { Points = 2000 };
        var state = QuizGames.Started([_round, second], _players);

        // When: Zoé is right everywhere, Max on the last question only
        state = QuizGames.Revealed(state, (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.B));
        state = QuizGames.Accepted(state, QuizGames.NextQuestion(state));
        state = QuizGames.Revealed(state, (1, QuizChoiceLetter.A));
        state = QuizGames.Accepted(state, QuizGames.NextQuestion(state));
        Assert.Equal(GamePhase.BetweenRounds, state.Phase);
        Assert.Equal([2000, 0, 0], state.Players.Select(p => p.Score));
        state = QuizGames.Accepted(state, Games.NextRound(state), seed: 43);
        state = QuizGames.Accepted(state, Games.StartRound(state));
        state = QuizGames.Revealed(state, (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.A));

        // Then
        Assert.Equal([4000, 2000, 0], state.Players.Select(p => p.Score));
    }

    [Fact]
    public void Handle_PlayerWhoJoinsDuringTheAnswers_StartsAtZeroAndScoresFromTheNextQuestion()
    {
        // Given: Noé joins once the answers of the first question are open
        var state = QuizGames.Accepted(QuizGames.Answering(Presented(_round), (1, QuizChoiceLetter.A)), Games.Join("Noé", player: 4));
        state = Reveal(state);
        Assert.Equal(0, state.Players[3].Score);
        Assert.False(QuizGames.RoundOf(state).Points.ContainsKey(Games.PlayerIdOf(4)));
        state = QuizGames.Accepted(state, QuizGames.NextQuestion(state));

        // When
        state = QuizGames.Revealed(state, (4, QuizChoiceLetter.A));

        // Then
        Assert.Equal([1000, 0, 0, 1000], state.Players.Select(p => p.Score));
    }

    [Fact]
    public void ProjectForPlayer_Revealed_ShowsThePointsOfTheQuestionAndTheNewScore()
    {
        // Given: Zoé is right after 10 seconds, having already scored on an earlier question; Max is wrong; Léa did not answer
        var state = AnsweredAfter(_fast, 10_000, earlier: 700);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.C));
        state = Reveal(state);

        // When
        var snapshots = state.Players.Select(p => QuizGames.Snapshots.ForPlayer(state, p)).ToArray();

        // Then
        Assert.Equal([1250, 0, 0], snapshots.Select(s => ((QuizPlayerView)s.RoundView!).Points));
        Assert.Equal([1950, 0, 0], snapshots.Select(s => s.Score));
    }

    [Fact]
    public void ProjectForPlayer_RevealedToAPlayerWhoDidNotTakePart_ShowsNoPointsAndTheirScore()
    {
        // Given
        var state = QuizGames.Accepted(QuizGames.Answering(Presented(_round), (1, QuizChoiceLetter.A)), Games.Join("Noé", player: 4));
        state = Reveal(state);

        // When
        var snapshot = QuizGames.Snapshots.ForPlayer(state, state.Players[3]);

        // Then
        Assert.Null(((QuizPlayerView)snapshot.RoundView!).Points);
        Assert.Equal(0, snapshot.Score);
    }

    [Theory]
    [InlineData(QuizPhase.Presentation)]
    [InlineData(QuizPhase.Answering)]
    [InlineData(QuizPhase.Locked)]
    public void Projections_BeforeTheReveal_ShowNoPointsOfTheQuestion(QuizPhase phase)
    {
        // Given
        var presented = Presented(_fast);
        var state = phase switch
        {
            QuizPhase.Presentation => presented,
            QuizPhase.Answering => QuizGames.Answering(presented, (1, QuizChoiceLetter.A)),
            _ => QuizGames.Locked(presented, (1, QuizChoiceLetter.A)),
        };

        // When
        var player = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView!;
        var gameMaster = (QuizGameMasterView)QuizGames.Snapshots.ForGameMaster(state).RoundView!;

        // Then
        Assert.Null(player.Points);
        Assert.All(gameMaster.Answers, answer => Assert.Null(answer.Points));
    }

    [Fact]
    public void ProjectForGameMaster_Revealed_ShowsThePointsOfEachParticipantAndEveryScore()
    {
        // Given: Noé joined too late to take part
        var state = QuizGames.Accepted(
            QuizGames.Answering(Presented(_round), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.B)),
            Games.Join("Noé", player: 4));
        state = Reveal(state);

        // When
        var snapshot = QuizGames.Snapshots.ForGameMaster(state);

        // Then
        var view = (QuizGameMasterView)snapshot.RoundView!;
        Assert.Equal([1000, 0, 0], view.Answers.Select(answer => answer.Points));
        Assert.Equal([("Zoé", 1000), ("Max", 0), ("Léa", 0), ("Noé", 0)], snapshot.Players.Select(p => (p.Nickname, p.Score)));
    }

    [Fact]
    public void ProjectForGameMaster_BetweenTwoRounds_StillShowsEveryScore()
    {
        // Given
        var state = QuizGames.Started([QuizGames.Round(QuizGames.CapitalQuestion), QuizGames.Round(QuizGames.LastQuestion)], _players);
        state = QuizGames.Revealed(state, (2, QuizChoiceLetter.A));
        state = QuizGames.Accepted(state, QuizGames.NextQuestion(state));

        // When
        var snapshot = QuizGames.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(GamePhase.BetweenRounds, state.Phase);
        Assert.Equal([0, 1000, 0], snapshot.Players.Select(p => p.Score));
    }

    /// <summary>The first question of the given round, presented to three players.</summary>
    private static GameState Presented(QuizRoundDescriptor round) => QuizGames.Started([round], _players);

    /// <summary>
    /// The first question of the given round, every choice shown, then answered right by Zoé, received the given time
    /// after the start of the countdown. With <paramref name="earlier"/>, Zoé has already scored that much on an earlier question.
    /// </summary>
    private static GameState AnsweredAfter(QuizRoundDescriptor round, int milliseconds, int earlier = 0)
    {
        var state = Presented(round);
        if (earlier > 0)
        {
            state = state with { Players = state.Players.SetItem(0, state.Players[0] with { Score = earlier }) };
        }

        state = QuizGames.Shown(state);
        return QuizGames.Accepted(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A, Games.Now.AddMilliseconds(milliseconds)));
    }

    /// <summary>The same game, its answers locked, then revealed.</summary>
    private static GameState Reveal(GameState state)
    {
        state = QuizGames.Closed(state);
        return QuizGames.Accepted(state, QuizGames.RevealAnswer(state));
    }
}
