using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// A quiz round of a game resumed after the server stopped: its countdown goes on with the time it had left when the game
/// was saved, and the answers already received keep their speed bonus.
/// </summary>
public sealed class QuizResumeTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    /// <summary>A round with a speed bonus, whose questions are answered in 20 seconds.</summary>
    private static readonly QuizRoundDescriptor _fast =
        QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.LastQuestion) with { Points = 1000, SpeedBonus = 500, AnswerSeconds = 20 };

    /// <summary>The game was saved 8 seconds into the countdown, which started at <see cref="Games.Now"/>.</summary>
    private static readonly DateTimeOffset _savedAt = Games.Now.AddSeconds(8);

    /// <summary>The game master resumes the game half an hour later.</summary>
    private static readonly DateTimeOffset _resumedAt = _savedAt.AddMinutes(30);

    public static TheoryData<string> StepsWithoutCountdown => ["presentation", "choices showing", "locked", "revealed"];

    [Fact]
    public void Handle_GameResumedWhileAnswering_GoesOnWithTheTimeLeftAndSchedulesTheCountdownAgain()
    {
        // Given: Zoé answered after 5 seconds
        var state = Answered(5_000);

        // When
        var transition = Resume(state, _savedAt);

        // Then: 12 seconds left, as when the game was saved
        Assert.Null(transition.Rejection);
        var round = QuizGames.RoundOf(transition.State);
        var closeAt = _resumedAt.AddSeconds(12);
        Assert.Equal((QuizPhase.Answering, closeAt), (round.Phase, round.AnswersCloseAt));
        Assert.Equal(_resumedAt.AddSeconds(-3), round.Answers[Games.PlayerIdOf(1)].ReceivedAt);
        Assert.Equal(
            new ScheduleTimer(QuizMode.AnswersTimer, closeAt) { RoundId = state.CurrentRound!.Id },
            Assert.Single(transition.Effects));
    }

    [Fact]
    public void Handle_GameResumedWhileAnswering_AnswersReceivedBeforeKeepTheirSpeedBonus()
    {
        // Given: Zoé answered after 5 seconds, which is worth 1375 points without any stop
        var state = Resume(Answered(5_000), _savedAt).State;

        // When
        var revealed = Reveal(state);

        // Then
        Assert.Equal(1375, revealed.Players[0].Score);
    }

    [Fact]
    public void Handle_GameResumedWhileAnswering_AnswersReceivedAfterAreScoredAgainstTheNewDeadline()
    {
        // Given: Max answers 2 seconds after the resumption, 10 seconds before the new deadline
        var state = Resume(Answered(5_000), _savedAt).State;
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.A, _resumedAt.AddSeconds(2)));

        // When
        var revealed = Reveal(state);

        // Then
        Assert.Equal(1250, revealed.Players[1].Score);
    }

    [Fact]
    public void Handle_GameResumedOnceTheDeadlineHadPassed_LocksTheAnswersAtOnce()
    {
        // Given: saved 2 seconds after the deadline, before the loop handled its timer
        var resumed = Resume(Answered(5_000), Games.Now.AddSeconds(22));
        var timer = Assert.IsType<ScheduleTimer>(Assert.Single(resumed.Effects));
        Assert.Equal(_resumedAt.AddSeconds(-2), timer.DueAt);

        // When: the timer, already due, elapses at once
        var locked = QuizGames.Engine.Handle(
            resumed.State,
            QuizGames.AnswersTimerElapsed(resumed.State, timer.DueAt),
            Context(_resumedAt));

        // Then
        Assert.Null(locked.Rejection);
        Assert.Equal(QuizPhase.Locked, QuizGames.RoundOf(locked.State).Phase);
    }

    [Fact]
    public void Handle_GameResumedOnceTheDeadlineHadPassed_RejectsAnAnswerAsTooLate()
    {
        // Given
        var state = Resume(Answered(5_000), Games.Now.AddSeconds(22)).State;

        // When
        var transition = QuizGames.Engine.Handle(
            state,
            QuizGames.Answer(state, 2, QuizChoiceLetter.A, _resumedAt),
            Context(_resumedAt));

        // Then
        Assert.Equal(RejectionReason.AnswerTooLate, transition.Rejection);
    }

    [Theory]
    [MemberData(nameof(StepsWithoutCountdown))]
    public void Handle_GameResumedWithoutCountdown_StaysAtTheSameStepWithoutTimer(string step)
    {
        // Given
        var state = step switch
        {
            "presentation" => Presented(),
            "choices showing" => QuizGames.Shown(Presented(), choiceCount: 2),
            "locked" => QuizGames.Locked(Presented(), (1, QuizChoiceLetter.A)),
            _ => QuizGames.Revealed(Presented(), (1, QuizChoiceLetter.A)),
        };
        var before = QuizGames.RoundOf(state);

        // When
        var transition = Resume(state, _savedAt);

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal(
            (before.QuestionIndex, before.Phase, before.QuestionShown, before.ShownChoiceCount, before.Answers.Count, before.Points),
            (round.QuestionIndex, round.Phase, round.QuestionShown, round.ShownChoiceCount, round.Answers.Count, round.Points));
        Assert.Equal(state.Players, transition.State.Players);
    }

    private static GameState Presented() => QuizGames.Started([_fast], _players);

    /// <summary>The countdown of the first question started at <see cref="Games.Now"/>, and Zoé answered right.</summary>
    private static GameState Answered(int receivedAfterMilliseconds)
    {
        var state = QuizGames.Shown(Presented());
        return QuizGames.Accepted(
            state,
            QuizGames.Answer(state, 1, QuizChoiceLetter.A, Games.Now.AddMilliseconds(receivedAfterMilliseconds)));
    }

    private static Transition Resume(GameState state, DateTimeOffset savedAt) =>
        QuizGames.Engine.Handle(state, new GameResumed(savedAt), Context(_resumedAt));

    /// <summary>The answers lock at the end of the countdown, then the game master reveals the answer.</summary>
    private static GameState Reveal(GameState state)
    {
        state = QuizGames.Closed(state);
        return QuizGames.Accepted(state, QuizGames.RevealAnswer(state));
    }

    private static GameContext Context(DateTimeOffset now) => new(now, new Random(42));
}
