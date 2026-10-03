using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.Quiz;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// The answers of a quiz question: opened by the game master, given by the players before the end of the countdown, then
/// locked by the timer or by the game master.
/// </summary>
public sealed class QuizAnswersTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly ImmutableArray<QuizRoundDescriptor> _rounds =
        [QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.LastQuestion)];

    private static readonly DateTimeOffset _closeAt = Games.Now.AddSeconds(QuizRoundDescriptor.DefaultAnswerSeconds);

    [Fact]
    public void Handle_OpenAnswers_StartsTheCountdownForThePlayersRegistered()
    {
        // Given
        var state = Presented();

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.OpenAnswers(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal((QuizPhase.Answering, _closeAt), (round.Phase, round.AnswersCloseAt));
        Assert.Equal([Games.PlayerIdOf(1), Games.PlayerIdOf(2), Games.PlayerIdOf(3)], round.Participants);
        Assert.Empty(round.Answers);
        var timer = Assert.IsType<ScheduleTimer>(Assert.Single(transition.Effects));
        Assert.Equal((QuizMode.AnswersTimer, _closeAt, state.CurrentRound!.Id), (timer.TimerId, timer.DueAt, timer.RoundId));
    }

    [Fact]
    public void Handle_OpenAnswers_RoundWithItsOwnTime_CountsDownFromIt()
    {
        // Given
        var state = QuizGames.Started([QuizGames.Round(QuizGames.CapitalQuestion) with { AnswerSeconds = 8 }], _players);

        // When
        var opened = QuizGames.Accepted(state, QuizGames.OpenAnswers(state));

        // Then
        Assert.Equal(Games.Now.AddSeconds(8), QuizGames.RoundOf(opened).AnswersCloseAt);
    }

    [Fact]
    public void Handle_OpenAnswers_QuestionWithItsOwnTime_CountsDownFromItRatherThanFromTheRound()
    {
        // Given
        var question = QuizGames.CapitalQuestion with { AnswerSeconds = 45 };
        var state = QuizGames.Started([QuizGames.Round(question) with { AnswerSeconds = 8 }], _players);

        // When
        var opened = QuizGames.Accepted(state, QuizGames.OpenAnswers(state));

        // Then
        Assert.Equal(Games.Now.AddSeconds(45), QuizGames.RoundOf(opened).AnswersCloseAt);
    }

    [Fact]
    public void Handle_OpenAnswers_DisconnectedPlayer_TakesPartAnyway()
    {
        // Given: Max's phone is asleep when the answers open
        var state = QuizGames.Accepted(Presented(), new PlayerConnectionLost(Games.PlayerIdOf(2)));

        // When
        var opened = QuizGames.Accepted(state, QuizGames.OpenAnswers(state));

        // Then
        Assert.Contains(Games.PlayerIdOf(2), QuizGames.RoundOf(opened).Participants);
    }

    [Fact]
    public void Handle_OpenAnswersTwice_IsRejectedAsObsolete()
    {
        // Given: two consoles, or a request sent again after a reconnection
        var state = QuizGames.Answering(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.OpenAnswers(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Fact]
    public void Handle_OpenAnswersOnceLocked_IsRejectedAsObsolete()
    {
        // Given
        var state = QuizGames.Locked(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.OpenAnswers(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Handle_OpenAnswersOfAnotherQuestion_IsRejected(int questionNumber)
    {
        // Given
        var state = Presented();

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.OpenAnswers(state, questionNumber), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.QuestionMismatch);
    }

    [Fact]
    public void Handle_OpenAnswersOfAnotherRound_IsRejected()
    {
        // Given
        var state = Presented();
        var open = new GameMasterRoundInput(new QuizOpenAnswers(new Contracts.RoundId(Guid.NewGuid()), 1), Games.Now);

        // When
        var transition = QuizGames.Engine.Handle(state, open, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.RoundMismatch);
    }

    [Fact]
    public void Handle_SubmitAnswer_RecordsTheChoiceAndWhenItWasReceived()
    {
        // Given
        var state = QuizGames.Answering(Presented());
        var receivedAt = Games.Now.AddSeconds(3);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 2, QuizChoiceLetter.C, receivedAt), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var answer = Assert.Single(QuizGames.RoundOf(transition.State).Answers);
        Assert.Equal((Games.PlayerIdOf(2), new QuizAnswer(QuizChoiceLetter.C, receivedAt)), (answer.Key, answer.Value));
        Assert.Equal(QuizPhase.Answering, QuizGames.RoundOf(transition.State).Phase);
    }

    [Fact]
    public void Handle_SubmitAnswer_EveryParticipantAnswered_KeepsTheAnswersOpen()
    {
        // Given: only the game master or the countdown locks the answers
        var state = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.B));

        // When
        var answered = QuizGames.Accepted(state, QuizGames.Answer(state, 3, QuizChoiceLetter.A));

        // Then
        Assert.Equal((QuizPhase.Answering, 3), (QuizGames.RoundOf(answered).Phase, QuizGames.RoundOf(answered).Answers.Count));
    }

    [Theory]
    [InlineData(QuizChoiceLetter.A)]
    [InlineData(QuizChoiceLetter.B)]
    public void Handle_SecondAnswer_IsRejectedAndTheFirstOneCounts(QuizChoiceLetter second)
    {
        // Given
        var state = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 1, second), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.AlreadyAnswered);
        Assert.Equal(QuizChoiceLetter.A, QuizGames.RoundOf(transition.State).Answers[Games.PlayerIdOf(1)].Choice);
    }

    [Fact]
    public void Handle_SubmitAnswerDuringThePresentation_IsRejected()
    {
        // Given
        var state = Presented();

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Fact]
    public void Handle_SubmitAnswerOnceLocked_IsRejected()
    {
        // Given
        var state = QuizGames.Locked(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Fact]
    public void Handle_SubmitAnswerOfAPlayerWhoJoinedAfterTheOpening_IsRejected()
    {
        // Given: Noé joins while the others answer, and plays from the next question
        var state = QuizGames.Accepted(QuizGames.Answering(Presented()), Games.Join("Noé", player: 4));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 4, QuizChoiceLetter.A), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.NotParticipating);
    }

    [Fact]
    public void Handle_SubmitAnswerOfAnUnregisteredPlayer_IsRejected()
    {
        // Given
        var state = QuizGames.Answering(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 9, QuizChoiceLetter.A), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PlayerUnknown);
    }

    [Fact]
    public void Handle_SubmitAnswerWithAChoiceTheQuestionDoesNotHave_IsRejected()
    {
        // Given: the question at stake has three choices, A to C
        var state = QuizGames.Answering(QuizGames.AtQuestion(Presented(), 1));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 1, QuizChoiceLetter.D), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.ChoiceUnknown);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Handle_SubmitAnswerToAnotherQuestion_IsRejected(int questionNumber)
    {
        // Given: an answer delayed until another question, or aimed at the wrong one
        var state = QuizGames.Answering(Presented());

        // When
        var transition = QuizGames.Engine.Handle(
            state,
            QuizGames.Answer(state, 1, QuizChoiceLetter.A, questionNumber: questionNumber),
            Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.QuestionMismatch);
    }

    [Fact]
    public void Handle_SubmitAnswerToAnotherRound_IsRejected()
    {
        // Given
        var state = QuizGames.Answering(Presented());
        var answer = new PlayerRoundInput(
            Games.PlayerIdOf(1),
            Games.NextClientSeq(state, 1),
            new QuizSubmitAnswer(new Contracts.RoundId(Guid.NewGuid()), 1, QuizChoiceLetter.A),
            Games.Now);

        // When
        var transition = QuizGames.Engine.Handle(state, answer, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.RoundMismatch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(250)]
    public void Handle_SubmitAnswerReceivedOnceClosedButBeforeTheTimer_IsRejected(int lateMilliseconds)
    {
        // Given: the answer reached the hub at or after the deadline, and the loop handles it before the timer
        var state = QuizGames.Answering(Presented());
        var receivedAt = _closeAt.AddMilliseconds(lateMilliseconds);

        // When
        var transition = QuizGames.Engine.Handle(
            state,
            QuizGames.Answer(state, 1, QuizChoiceLetter.A, receivedAt),
            new GameContext(receivedAt.AddMilliseconds(5), new Random(42)));

        // Then
        AssertRejected(state, transition, RejectionReason.AnswerTooLate);
    }

    [Fact]
    public void Handle_SubmitAnswerReceivedBeforeTheDeadlineButHandledAfterIt_IsAccepted()
    {
        // Given: the queue of the loop delayed an answer that arrived in time
        var state = QuizGames.Answering(Presented());
        var receivedAt = _closeAt.AddMilliseconds(-1);

        // When
        var transition = QuizGames.Engine.Handle(
            state,
            QuizGames.Answer(state, 1, QuizChoiceLetter.B, receivedAt),
            new GameContext(_closeAt.AddMilliseconds(30), new Random(42)));

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(receivedAt, QuizGames.RoundOf(transition.State).Answers[Games.PlayerIdOf(1)].ReceivedAt);
    }

    [Fact]
    public void Handle_AnswersTimerElapsed_LocksTheAnswers()
    {
        // Given
        var state = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.AnswersTimerElapsed(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal(QuizPhase.Locked, round.Phase);
        Assert.Equal(QuizChoiceLetter.A, round.Answers[Games.PlayerIdOf(1)].Choice);
    }

    [Fact]
    public void Handle_AnswersTimerElapsedOnceLockedByTheGameMaster_IsRejectedAsObsolete()
    {
        // Given: the timer elapsed while the lock of the game master was being handled
        var answering = QuizGames.Answering(Presented());
        var state = QuizGames.Accepted(answering, QuizGames.LockAnswers(answering));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.AnswersTimerElapsed(answering), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.UnexpectedTimer);
    }

    [Fact]
    public void Handle_TimerDueAtAnotherTime_IsRejectedAsObsolete()
    {
        // Given: a timer of answers opened earlier, replaced since
        var state = QuizGames.Answering(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.AnswersTimerElapsed(state, _closeAt.AddSeconds(-5)), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.UnexpectedTimer);
    }

    [Fact]
    public void Handle_AnotherTimerOfTheRound_IsRejected()
    {
        // Given
        var state = QuizGames.Answering(Presented());
        var timer = new TimerElapsed(new TimerId("another"), _closeAt) { RoundId = state.CurrentRound!.Id };

        // When
        var transition = QuizGames.Engine.Handle(state, timer, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.UnexpectedTimer);
    }

    [Fact]
    public void Handle_LockAnswers_LocksThemAndCancelsTheTimer()
    {
        // Given
        var state = QuizGames.Answering(Presented(), (2, QuizChoiceLetter.B));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.LockAnswers(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(new CancelTimer(QuizMode.AnswersTimer), Assert.Single(transition.Effects));
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal((QuizPhase.Locked, _closeAt), (round.Phase, round.AnswersCloseAt));
        Assert.Single(round.Answers);
    }

    [Fact]
    public void Handle_LockAnswersDuringThePresentation_IsRejected()
    {
        // Given
        var state = Presented();

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.LockAnswers(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Fact]
    public void Handle_LockAnswersTwice_IsRejectedAsObsolete()
    {
        // Given
        var state = QuizGames.Locked(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.LockAnswers(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Handle_LockAnswersOfAnotherQuestion_IsRejected(int questionNumber)
    {
        // Given
        var state = QuizGames.Answering(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.LockAnswers(state, questionNumber), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.QuestionMismatch);
    }

    [Fact]
    public void ProjectForDisplay_Answering_ShowsTheDeadlineAndHowManyAnsweredOnly()
    {
        // Given
        var state = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.B), (3, QuizChoiceLetter.D));

        // When
        var view = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;

        // Then
        Assert.Equal(QuizQuestionPhase.Answering, view.Phase);
        Assert.Equal(_closeAt.ToUnixTimeMilliseconds(), view.AnswersCloseAt);
        Assert.Equal((2, 3), (view.AnsweredCount, view.ParticipantCount));
    }

    [Fact]
    public void ProjectForDisplay_Presentation_HasNeitherDeadlineNorCount()
    {
        // Given
        var state = Presented();

        // When
        var view = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;

        // Then
        Assert.Equal((null, 0, 0), (view.AnswersCloseAt, view.AnsweredCount, view.ParticipantCount));
    }

    [Fact]
    public void ProjectForDisplay_PlayerJoinedAfterTheOpening_IsNotCounted()
    {
        // Given
        var state = QuizGames.Accepted(QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A)), Games.Join("Noé", player: 4));

        // When
        var view = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;

        // Then
        Assert.Equal((1, 3), (view.AnsweredCount, view.ParticipantCount));
    }

    [Fact]
    public void Projections_Locked_ShowNoDeadlineAnymore()
    {
        // Given
        var state = QuizGames.Locked(Presented(), (1, QuizChoiceLetter.A));

        // When
        var display = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;
        var gameMaster = (QuizGameMasterView)QuizGames.Snapshots.ForGameMaster(state).RoundView!;
        var player = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView!;

        // Then
        Assert.Equal((QuizQuestionPhase.Locked, null, 1, 3), (display.Phase, display.AnswersCloseAt, display.AnsweredCount, display.ParticipantCount));
        Assert.Equal((QuizQuestionPhase.Locked, null), (gameMaster.Phase, gameMaster.AnswersCloseAt));
        Assert.Equal((QuizQuestionPhase.Locked, null, QuizChoiceLetter.A), (player.Phase, player.AnswersCloseAt, player.Answer));
    }

    [Fact]
    public void ProjectForPlayer_Answering_ShowsTheDeadlineAndTheOwnChoiceOnly()
    {
        // Given
        var state = QuizGames.Answering(Presented(), (2, QuizChoiceLetter.C));

        // When
        var max = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[1]).RoundView!;
        var zoe = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView!;

        // Then
        Assert.Equal((QuizQuestionPhase.Answering, _closeAt.ToUnixTimeMilliseconds(), true, QuizChoiceLetter.C), (max.Phase, max.AnswersCloseAt, max.Participating, max.Answer));
        Assert.Equal((true, null), (zoe.Participating, zoe.Answer));
    }

    [Fact]
    public void ProjectForPlayer_JoinedAfterTheOpening_DoesNotTakePart()
    {
        // Given
        var state = QuizGames.Accepted(QuizGames.Answering(Presented()), Games.Join("Noé", player: 4));

        // When
        var view = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[3]).RoundView!;

        // Then
        Assert.Equal((QuizQuestionPhase.Answering, false, null), (view.Phase, view.Participating, view.Answer));
        Assert.Equal(_closeAt.ToUnixTimeMilliseconds(), view.AnswersCloseAt);
    }

    [Fact]
    public void ProjectForPlayer_Presentation_TakesPart()
    {
        // Given
        var state = Presented();

        // When
        var view = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView!;

        // Then
        Assert.Equal((true, null, null), (view.Participating, view.AnswersCloseAt, view.Answer));
    }

    [Fact]
    public void ProjectForGameMaster_Answering_ShowsEachParticipantWithTheirChoiceAndTheCountOfEachChoice()
    {
        // Given: Léa answers first, Zoé has not answered, and Noé joins too late to take part
        var state = QuizGames.Answering(Presented(), (3, QuizChoiceLetter.B), (2, QuizChoiceLetter.B));
        state = QuizGames.Accepted(state, Games.Join("Noé", player: 4));

        // When
        var view = (QuizGameMasterView)QuizGames.Snapshots.ForGameMaster(state).RoundView!;

        // Then: in order of arrival, not of answer
        Assert.Equal(
            [
                new QuizGameMasterAnswer(Games.PlayerIdOf(1), "Zoé", null, null),
                new QuizGameMasterAnswer(Games.PlayerIdOf(2), "Max", QuizChoiceLetter.B, null),
                new QuizGameMasterAnswer(Games.PlayerIdOf(3), "Léa", QuizChoiceLetter.B, null),
            ],
            view.Answers);
        Assert.Equal([0, 2, 0, 0], view.Choices.Select(choice => choice.AnswerCount));
        Assert.Equal(_closeAt.ToUnixTimeMilliseconds(), view.AnswersCloseAt);
    }

    [Fact]
    public void ProjectForGameMaster_ParticipantRenamed_ShowsTheirCurrentNickname()
    {
        // Given
        var state = QuizGames.Accepted(QuizGames.Answering(Presented(), (2, QuizChoiceLetter.A)), Games.Rename(2, "Maxime"));

        // When
        var view = (QuizGameMasterView)QuizGames.Snapshots.ForGameMaster(state).RoundView!;

        // Then
        Assert.Equal("Maxime", view.Answers[1].Nickname);
    }

    [Fact]
    public void ProjectForGameMaster_Presentation_ListsNoAnswerYet()
    {
        // Given
        var state = Presented();

        // When
        var view = (QuizGameMasterView)QuizGames.Snapshots.ForGameMaster(state).RoundView!;

        // Then
        Assert.Empty(view.Answers);
        Assert.All(view.Choices, choice => Assert.Equal(0, choice.AnswerCount));
        Assert.Null(view.AnswersCloseAt);
    }

    /// <summary>The first question of a round of two, presented to three players.</summary>
    private static GameState Presented() => QuizGames.Started(_rounds, _players);

    private static void AssertRejected(GameState state, Transition transition, RejectionReason reason)
    {
        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }
}
