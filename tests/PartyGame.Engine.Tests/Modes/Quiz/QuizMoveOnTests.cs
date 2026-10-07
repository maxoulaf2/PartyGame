using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// How a quiz round moves from one question to the next: the game master moves on once a question is revealed, or skips
/// it before, and the round ends after its last question.
/// </summary>
public sealed class QuizMoveOnTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    /// <summary>A first round of three questions, then a second one, so that the end of the first one is not the last.</summary>
    private static readonly ImmutableArray<QuizRoundDescriptor> _rounds =
    [
        QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.IllustratedQuestion, QuizGames.LastQuestion),
        QuizGames.Round(QuizGames.CapitalQuestion),
    ];

    public static TheoryData<QuizPhase> PhasesBeforeTheReveal => [QuizPhase.Presentation, QuizPhase.Answering, QuizPhase.Locked];

    [Fact]
    public void Handle_NextQuestion_PresentsTheNextQuestionWithoutAnyAnswer()
    {
        // Given
        var state = QuizGames.Revealed(Presented(), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.B));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.NextQuestion(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(GamePhase.Round, transition.State.Phase);
        Assert.Equal(state.CurrentRound!.Id, transition.State.CurrentRound!.Id);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal((1, 2, QuizPhase.Presentation), (round.QuestionIndex, round.QuestionNumber, round.Phase));
        Assert.Equal([0, 1], round.ChoiceOrder);
        Assert.Null(round.AnswersCloseAt);
        Assert.Empty(round.Participants);
        Assert.Empty(round.Answers);
        Assert.Empty(round.SkippedQuestions);
    }

    [Fact]
    public void Handle_NextQuestionWithShuffle_DrawsTheOrderOfTheNextQuestionFromTheSeed()
    {
        // Given
        var shuffled = _rounds[0] with { ShuffleChoices = true };
        var state = QuizGames.Revealed(QuizGames.Started([shuffled], _players));

        // When
        var once = QuizGames.RoundOf(QuizGames.Accepted(state, QuizGames.NextQuestion(state), seed: 7));
        var again = QuizGames.RoundOf(QuizGames.Accepted(state, QuizGames.NextQuestion(state), seed: 7));

        // Then
        Assert.Equal(2, once.QuestionNumber);
        Assert.Equal(once.ChoiceOrder, again.ChoiceOrder);
        Assert.Equal([0, 1], once.ChoiceOrder.Order());
    }

    [Fact]
    public void Handle_NextQuestion_PlayerWhoJoinedDuringTheAnswers_TakesPartInTheNextQuestion()
    {
        // Given: Noé joins too late for the first question
        var state = QuizGames.Accepted(QuizGames.Answering(Presented()), Games.Join("Noé", player: 4));
        state = QuizGames.Closed(state);
        state = QuizGames.Accepted(state, QuizGames.RevealAnswer(state));

        // When
        state = QuizGames.Accepted(state, QuizGames.NextQuestion(state));
        state = QuizGames.Shown(state);

        // Then
        Assert.Contains(Games.PlayerIdOf(4), QuizGames.RoundOf(state).Participants);
        Assert.Null(QuizGames.Engine.Handle(state, QuizGames.Answer(state, 4, QuizChoiceLetter.B), Games.Context()).Rejection);
    }

    [Fact]
    public void Handle_NextQuestionAfterTheLastQuestion_EndsTheRound()
    {
        // Given
        var state = QuizGames.Revealed(QuizGames.AtQuestion(Presented(), 2), (1, QuizChoiceLetter.A));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.NextQuestion(state), Games.Context());

        // Then: the round stays on its last question, for its history
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(GamePhase.BetweenRounds, transition.State.Phase);
        Assert.Equal(state.CurrentRound!.Id, transition.State.CurrentRound!.Id);
        Assert.Equal((3, QuizPhase.Revealed), (QuizGames.RoundOf(transition.State).QuestionNumber, QuizGames.RoundOf(transition.State).Phase));
    }

    [Fact]
    public void Handle_NextQuestionAfterTheLastQuestionOfTheLastRound_FinishesTheGame()
    {
        // Given
        var state = QuizGames.Revealed(QuizGames.Started([QuizGames.Round(QuizGames.CapitalQuestion)], _players));

        // When
        var finished = QuizGames.Accepted(state, QuizGames.NextQuestion(state));

        // Then
        Assert.Equal(GamePhase.Finished, finished.Phase);
    }

    [Theory]
    [MemberData(nameof(PhasesBeforeTheReveal))]
    public void Handle_NextQuestionBeforeTheReveal_IsRejected(QuizPhase phase)
    {
        // Given: the game master reveals first, or skips the question
        var state = InPhase(Presented(), phase);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.NextQuestion(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Fact]
    public void Handle_NextQuestionTwice_IsRejectedAsObsoleteAndSkipsNoQuestion()
    {
        // Given: two consoles, or a request sent again after a reconnection, both aimed at the first question
        var revealed = QuizGames.Revealed(Presented());
        var next = QuizGames.NextQuestion(revealed);
        var state = QuizGames.Accepted(revealed, next);

        // When
        var transition = QuizGames.Engine.Handle(state, next, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.QuestionMismatch);
        Assert.Equal(2, QuizGames.RoundOf(state).QuestionNumber);
    }

    [Fact]
    public void Handle_NextQuestionTwiceAfterTheLastQuestion_IsRejectedOnceTheRoundIsOver()
    {
        // Given
        var revealed = QuizGames.Revealed(QuizGames.AtQuestion(Presented(), 2));
        var next = QuizGames.NextQuestion(revealed);
        var state = QuizGames.Accepted(revealed, next);

        // When
        var transition = QuizGames.Engine.Handle(state, next, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.NotInRound);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Handle_NextQuestionAfterAnotherQuestion_IsRejected(int questionNumber)
    {
        // Given
        var state = QuizGames.Revealed(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.NextQuestion(state, questionNumber), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.QuestionMismatch);
    }

    [Fact]
    public void Handle_NextQuestionOfAnotherRound_IsRejected()
    {
        // Given
        var state = QuizGames.Revealed(Presented());
        var next = new GameMasterRoundInput(new QuizNextQuestion(new Contracts.RoundId(Guid.NewGuid()), 1), Games.Now);

        // When
        var transition = QuizGames.Engine.Handle(state, next, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.RoundMismatch);
    }

    [Theory]
    [MemberData(nameof(PhasesBeforeTheReveal))]
    public void Handle_SkipQuestion_PresentsTheNextQuestionAndKeepsTheSkippedOneMarked(QuizPhase phase)
    {
        // Given
        var state = InPhase(Presented(), phase);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.SkipQuestion(state), Games.Context());

        // Then: the answers received are ignored
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Round, transition.State.Phase);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal((2, QuizPhase.Presentation), (round.QuestionNumber, round.Phase));
        Assert.Empty(round.Answers);
        Assert.Empty(round.Participants);
        Assert.Equal([0], round.SkippedQuestions);
    }

    [Fact]
    public void Handle_SkipQuestionWhileTheAnswersAreOpen_CancelsTheirTimer()
    {
        // Given
        var state = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.SkipQuestion(state), Games.Context());

        // Then
        Assert.Equal([new CancelTimer(QuizMode.AnswersTimer)], transition.Effects);
    }

    [Theory]
    [InlineData(QuizPhase.Presentation)]
    [InlineData(QuizPhase.Locked)]
    public void Handle_SkipQuestionWithoutCountdown_HasNoEffect(QuizPhase phase)
    {
        // Given
        var state = InPhase(Presented(), phase);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.SkipQuestion(state), Games.Context());

        // Then
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Handle_TimerOfTheSkippedQuestion_IsRejectedAsObsolete()
    {
        // Given: the timer elapsed before its cancellation reached it
        var answering = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A));
        var timer = QuizGames.AnswersTimerElapsed(answering);
        var state = QuizGames.Accepted(answering, QuizGames.SkipQuestion(answering));

        // When
        var transition = QuizGames.Engine.Handle(state, timer, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.UnexpectedTimer);
    }

    [Fact]
    public void Handle_AnswerToTheSkippedQuestion_IsRejected()
    {
        // Given: an answer delayed by the network until the next question
        var answering = QuizGames.Answering(Presented());
        var late = QuizGames.Answer(answering, 1, QuizChoiceLetter.A);
        var skipped = QuizGames.Accepted(answering, QuizGames.SkipQuestion(answering));
        var state = QuizGames.Shown(skipped);

        // When
        var transition = QuizGames.Engine.Handle(state, late, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.QuestionMismatch);
    }

    [Fact]
    public void Handle_SkipQuestionsInARow_KeepsEveryOneMarked()
    {
        // Given: the second question is played, then the third one skipped too
        var state = QuizGames.Accepted(Presented(), QuizGames.SkipQuestion(Presented()));
        state = QuizGames.Revealed(state);
        state = QuizGames.Accepted(state, QuizGames.NextQuestion(state));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.SkipQuestion(state), Games.Context());

        // Then: the third question was the last one
        Assert.Equal(GamePhase.BetweenRounds, transition.State.Phase);
        Assert.Equal([0, 2], QuizGames.RoundOf(transition.State).SkippedQuestions);
    }

    [Theory]
    [MemberData(nameof(PhasesBeforeTheReveal))]
    public void Handle_SkipTheLastQuestion_EndsTheRound(QuizPhase phase)
    {
        // Given
        var state = InPhase(QuizGames.AtQuestion(Presented(), 2), phase);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.SkipQuestion(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.BetweenRounds, transition.State.Phase);
        Assert.Equal([2], QuizGames.RoundOf(transition.State).SkippedQuestions);
        Assert.Equal(phase == QuizPhase.Answering ? [new CancelTimer(QuizMode.AnswersTimer)] : [], transition.Effects);
    }

    [Fact]
    public void Handle_SkipQuestionOnceRevealed_IsRejected()
    {
        // Given: the game master moves on to the next question instead
        var state = QuizGames.Revealed(Presented(), (1, QuizChoiceLetter.A));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.SkipQuestion(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Fact]
    public void Handle_SkipQuestionTwice_IsRejectedAsObsoleteAndSkipsNoOtherQuestion()
    {
        // Given: two consoles, or a request sent again after a reconnection, both aimed at the first question
        var presented = Presented();
        var skip = QuizGames.SkipQuestion(presented);
        var state = QuizGames.Accepted(presented, skip);

        // When
        var transition = QuizGames.Engine.Handle(state, skip, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.QuestionMismatch);
        Assert.Equal(2, QuizGames.RoundOf(state).QuestionNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Handle_SkipAnotherQuestion_IsRejected(int questionNumber)
    {
        // Given
        var state = QuizGames.Answering(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.SkipQuestion(state, questionNumber), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.QuestionMismatch);
    }

    [Fact]
    public void Handle_SkipQuestionOfAnotherRound_IsRejected()
    {
        // Given
        var state = Presented();
        var skip = new GameMasterRoundInput(new QuizSkipQuestion(new Contracts.RoundId(Guid.NewGuid()), 1), Games.Now);

        // When
        var transition = QuizGames.Engine.Handle(state, skip, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.RoundMismatch);
    }

    [Fact]
    public void Projections_AfterASkippedQuestion_NumberTheNextOneWithoutMentioningIt()
    {
        // Given: the first of three questions is skipped
        var locked = QuizGames.Locked(Presented(), (1, QuizChoiceLetter.B));
        var state = QuizGames.Accepted(locked, QuizGames.SkipQuestion(locked));

        // When
        var display = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;
        var gameMaster = (QuizGameMasterView)QuizGames.Snapshots.ForGameMaster(state).RoundView!;
        var player = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView!;

        // Then
        Assert.Equal((2, 3, QuizQuestionPhase.Presentation), (display.QuestionNumber, display.QuestionCount, display.Phase));
        Assert.Null(display.Text);
        Assert.Equal(QuizGames.IllustratedQuestion.Text, gameMaster.Text);
        Assert.Equal((2, 3, QuizQuestionPhase.Presentation), (gameMaster.QuestionNumber, gameMaster.QuestionCount, gameMaster.Phase));
        Assert.Empty(gameMaster.Answers);
        Assert.Equal((2, 3, null, true), (player.QuestionNumber, player.QuestionCount, player.Answer, player.Participating));
    }

    /// <summary>The first question of a round of three, presented to three players.</summary>
    private static GameState Presented() => QuizGames.Started(_rounds, _players);

    /// <summary>The same game, its question in progress brought to <paramref name="phase"/>, Zoé having answered A.</summary>
    private static GameState InPhase(GameState state, QuizPhase phase) => phase switch
    {
        QuizPhase.Presentation => state,
        QuizPhase.Answering => QuizGames.Answering(state, (1, QuizChoiceLetter.A)),
        QuizPhase.Locked => QuizGames.Locked(state, (1, QuizChoiceLetter.A)),
        _ => QuizGames.Revealed(state, (1, QuizChoiceLetter.A)),
    };

    private static void AssertRejected(GameState state, Transition transition, RejectionReason reason)
    {
        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }
}
