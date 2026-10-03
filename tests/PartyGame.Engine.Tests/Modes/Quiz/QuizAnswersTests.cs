using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.Quiz;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// The answers of a quiz question: opened with its first choice shown, given by the players as the choices show then
/// before the end of the countdown, which starts with the last choice, then locked by the timer, or as soon as every
/// participant answered and every choice is shown.
/// </summary>
public sealed class QuizAnswersTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly ImmutableArray<QuizRoundDescriptor> _rounds =
        [QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.LastQuestion)];

    private static readonly DateTimeOffset _closeAt = Games.Now.AddSeconds(QuizRoundDescriptor.DefaultAnswerSeconds);

    [Fact]
    public void Handle_ShowChoice_First_OpensTheAnswersToThePlayersRegistered()
    {
        // Given
        var state = QuizGames.Shown(Presented(), choiceCount: 0);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, QuizChoiceLetter.A), Games.Context());

        // Then: the countdown waits for the last choice
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal((QuizPhase.Presentation, null), (round.Phase, round.AnswersCloseAt));
        Assert.Equal([Games.PlayerIdOf(1), Games.PlayerIdOf(2), Games.PlayerIdOf(3)], round.Participants);
        Assert.Empty(round.Answers);
    }

    [Fact]
    public void Handle_ShowChoice_Last_StartsTheCountdownOfTheParticipants()
    {
        // Given: Noé joined once the first choice showed, too late to take part
        var state = QuizGames.Accepted(QuizGames.Shown(Presented(), choiceCount: 1), Games.Join("Noé", player: 4));
        state = QuizGames.Shown(state, choiceCount: 3);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, QuizChoiceLetter.D), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal((QuizPhase.Answering, _closeAt), (round.Phase, round.AnswersCloseAt));
        Assert.Equal([Games.PlayerIdOf(1), Games.PlayerIdOf(2), Games.PlayerIdOf(3)], round.Participants);
        var timer = Assert.IsType<ScheduleTimer>(Assert.Single(transition.Effects));
        Assert.Equal((QuizMode.AnswersTimer, _closeAt, state.CurrentRound!.Id), (timer.TimerId, timer.DueAt, timer.RoundId));
    }

    [Fact]
    public void Handle_ShowChoice_Last_RoundWithItsOwnTime_CountsDownFromIt()
    {
        // Given
        var state = QuizGames.Started([QuizGames.Round(QuizGames.CapitalQuestion) with { AnswerSeconds = 8 }], _players);

        // When
        var shown = QuizGames.Shown(state);

        // Then
        Assert.Equal(Games.Now.AddSeconds(8), QuizGames.RoundOf(shown).AnswersCloseAt);
    }

    [Fact]
    public void Handle_ShowChoice_Last_QuestionWithItsOwnTime_CountsDownFromItRatherThanFromTheRound()
    {
        // Given
        var question = QuizGames.CapitalQuestion with { AnswerSeconds = 45 };
        var state = QuizGames.Started([QuizGames.Round(question) with { AnswerSeconds = 8 }], _players);

        // When
        var shown = QuizGames.Shown(state);

        // Then
        Assert.Equal(Games.Now.AddSeconds(45), QuizGames.RoundOf(shown).AnswersCloseAt);
    }

    [Fact]
    public void Handle_ShowChoice_First_DisconnectedPlayer_TakesPartAnyway()
    {
        // Given: Max's phone is asleep when the answers open
        var state = QuizGames.Accepted(Presented(), new PlayerConnectionLost(Games.PlayerIdOf(2)));

        // When
        var opened = QuizGames.Shown(state, choiceCount: 1);

        // Then
        Assert.Contains(Games.PlayerIdOf(2), QuizGames.RoundOf(opened).Participants);
    }

    [Fact]
    public void Handle_ShowChoice_Last_EverybodyAnsweredAlready_LocksTheAnswersWithoutCountdown()
    {
        // Given: everybody answered while the choices showed
        var state = QuizGames.Shown(Presented(), choiceCount: 3);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A));
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.C));
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 3, QuizChoiceLetter.B));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, QuizChoiceLetter.D), Games.Context());

        // Then: every choice shows, and the game master may reveal at once
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal((QuizPhase.Locked, null, 4), (round.Phase, round.AnswersCloseAt, round.ShownChoiceCount));
        Assert.Equal(3, round.Answers.Count);
    }

    [Fact]
    public void Handle_ShowChoice_Last_DisconnectedParticipantLeftToAnswer_StartsTheCountdown()
    {
        // Given: Léa's phone fell asleep once the answers opened, and the others answered
        var state = QuizGames.Shown(Presented(), choiceCount: 2);
        state = QuizGames.Accepted(state, new PlayerConnectionLost(Games.PlayerIdOf(3)));
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A));
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.B));

        // When
        var round = QuizGames.RoundOf(QuizGames.Shown(state));

        // Then: she can still answer once back
        Assert.Equal((QuizPhase.Answering, _closeAt), (round.Phase, round.AnswersCloseAt));
    }

    [Fact]
    public void Handle_ShowChoiceOfAnotherRound_IsRejected()
    {
        // Given
        var state = QuizGames.Shown(Presented(), choiceCount: 0);
        var show = new GameMasterRoundInput(new QuizShowChoice(new Contracts.RoundId(Guid.NewGuid()), 1, QuizChoiceLetter.A), Games.Now);

        // When
        var transition = QuizGames.Engine.Handle(state, show, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.RoundMismatch);
    }

    [Fact]
    public void Handle_SubmitAnswerWhileTheChoicesShow_RecordsItWithoutLockingTheAnswers()
    {
        // Given: two choices of four shown, and Zoé answered already
        var state = QuizGames.Shown(Presented(), choiceCount: 2);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A));
        var receivedAt = Games.Now.AddSeconds(-3);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 2, QuizChoiceLetter.B, receivedAt), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal(QuizPhase.Presentation, round.Phase);
        Assert.Equal(new QuizAnswer(QuizChoiceLetter.B, receivedAt), round.Answers[Games.PlayerIdOf(2)]);
    }

    [Fact]
    public void Handle_SubmitAnswerOfTheLastParticipantWhileTheChoicesShow_KeepsShowingThem()
    {
        // Given
        var state = QuizGames.Shown(Presented(), choiceCount: 2);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A));
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.B));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 3, QuizChoiceLetter.A), Games.Context());

        // Then: the game master reads out the other choices, the last one locking the answers
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(QuizPhase.Presentation, QuizGames.RoundOf(transition.State).Phase);
        Assert.Null(QuizGames.Engine.Handle(transition.State, QuizGames.ShowChoice(transition.State, QuizChoiceLetter.C), Games.Context()).Rejection);
    }

    [Fact]
    public void Handle_SubmitAnswerWithAChoiceNotShownYet_IsRejected()
    {
        // Given: A and B show, C and D not yet
        var state = QuizGames.Shown(Presented(), choiceCount: 2);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 1, QuizChoiceLetter.C), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.ChoiceHidden);
    }

    [Fact]
    public void Handle_SubmitAnswerOfAPlayerWhoJoinedAfterTheFirstChoice_IsRejected()
    {
        // Given: Noé joins once the answers opened, with the first choice
        var state = QuizGames.Accepted(QuizGames.Shown(Presented(), choiceCount: 1), Games.Join("Noé", player: 4));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 4, QuizChoiceLetter.A), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.NotParticipating);
    }

    [Fact]
    public void Handle_SubmitAnswerOfAPlayerWhoJoinedBeforeTheFirstChoice_IsAccepted()
    {
        // Given: Noé joins while the question shows, its choices not yet
        var state = QuizGames.Accepted(QuizGames.Shown(Presented(), choiceCount: 0), Games.Join("Noé", player: 4));
        state = QuizGames.Shown(state, choiceCount: 1);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 4, QuizChoiceLetter.A), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
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
    public void Handle_SubmitAnswerOfTheLastParticipant_LocksTheAnswersAndCancelsTheTimer()
    {
        // Given
        var state = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.B));
        var receivedAt = Games.Now.AddSeconds(4);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 3, QuizChoiceLetter.A, receivedAt), Games.Context());

        // Then: the answer counts, and the deadline stays, for the speed bonus
        Assert.Null(transition.Rejection);
        Assert.Equal(new CancelTimer(QuizMode.AnswersTimer), Assert.Single(transition.Effects));
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal((QuizPhase.Locked, _closeAt, 3), (round.Phase, round.AnswersCloseAt, round.Answers.Count));
        Assert.Equal(new QuizAnswer(QuizChoiceLetter.A, receivedAt), round.Answers[Games.PlayerIdOf(3)]);
    }

    [Fact]
    public void Handle_SubmitAnswer_DisconnectedParticipantLeftToAnswer_KeepsTheAnswersOpen()
    {
        // Given: Léa's phone fell asleep once the answers opened, and she takes part anyway
        var state = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A));
        state = QuizGames.Accepted(state, new PlayerConnectionLost(Games.PlayerIdOf(3)));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 2, QuizChoiceLetter.B), Games.Context());

        // Then: the countdown goes on, so that she can still answer once back
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(QuizPhase.Answering, QuizGames.RoundOf(transition.State).Phase);
    }

    [Fact]
    public void Handle_SubmitAnswerOfTheLastParticipant_PlayerJoinedDuringTheAnswers_LocksThemAnyway()
    {
        // Given: Noé joined once the answers opened, too late to take part
        var state = QuizGames.Accepted(QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.B)), Games.Join("Noé", player: 4));

        // When
        var answered = QuizGames.Accepted(state, QuizGames.Answer(state, 3, QuizChoiceLetter.C));

        // Then
        Assert.Equal(QuizPhase.Locked, QuizGames.RoundOf(answered).Phase);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Handle_SubmitAnswerBeforeTheFirstChoice_IsRejected(bool questionShown)
    {
        // Given
        var state = questionShown ? QuizGames.Shown(Presented(), choiceCount: 0) : Presented();

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
    public void Handle_AnswersTimerElapsedOnceEverybodyAnswered_IsRejectedAsObsolete()
    {
        // Given: the timer elapsed while the last answer was being handled
        var answering = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.B));
        var state = QuizGames.Accepted(answering, QuizGames.Answer(answering, 3, QuizChoiceLetter.C));

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
    public void ProjectForDisplay_ChoicesShowing_ShowsHowManyAnsweredWithoutDeadline()
    {
        // Given
        var state = QuizGames.Shown(Presented(), choiceCount: 2);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.B));

        // When
        var view = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;

        // Then
        Assert.Equal((QuizQuestionPhase.Presentation, null, 1, 3), (view.Phase, view.AnswersCloseAt, view.AnsweredCount, view.ParticipantCount));
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
        Assert.Equal((true, null, null, 0), (view.Participating, view.AnswersCloseAt, view.Answer, view.ShownChoiceCount));
    }

    [Fact]
    public void ProjectForPlayer_ChoicesShowing_UnlocksTheChoicesShownAndShowsTheOwnChoiceOnly()
    {
        // Given
        var state = QuizGames.Shown(Presented(), choiceCount: 2);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 2, QuizChoiceLetter.B));

        // When
        var max = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[1]).RoundView!;
        var zoe = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView!;

        // Then: every button shows, the first two unlocked
        Assert.Equal([QuizChoiceLetter.A, QuizChoiceLetter.B, QuizChoiceLetter.C, QuizChoiceLetter.D], max.Choices);
        Assert.Equal((QuizQuestionPhase.Presentation, 2, null, true, QuizChoiceLetter.B), (max.Phase, max.ShownChoiceCount, max.AnswersCloseAt, max.Participating, max.Answer));
        Assert.Equal((true, null), (zoe.Participating, zoe.Answer));
    }

    [Fact]
    public void ProjectForPlayer_JoinedAfterTheFirstChoice_DoesNotTakePart()
    {
        // Given
        var state = QuizGames.Accepted(QuizGames.Shown(Presented(), choiceCount: 1), Games.Join("Noé", player: 4));

        // When
        var view = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[3]).RoundView!;

        // Then
        Assert.Equal((QuizQuestionPhase.Presentation, false), (view.Phase, view.Participating));
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
    public void ProjectForGameMaster_ChoicesShowing_ShowsEachParticipantWithTheirChoice()
    {
        // Given
        var state = QuizGames.Shown(Presented(), choiceCount: 1);
        state = QuizGames.Accepted(state, QuizGames.Answer(state, 3, QuizChoiceLetter.A));

        // When
        var view = (QuizGameMasterView)QuizGames.Snapshots.ForGameMaster(state).RoundView!;

        // Then
        Assert.Equal([null, null, QuizChoiceLetter.A], view.Answers.Select(answer => answer.Choice));
        Assert.Equal([1, 0, 0, 0], view.Choices.Select(choice => choice.AnswerCount));
        Assert.Null(view.AnswersCloseAt);
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
