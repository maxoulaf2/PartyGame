using System.Collections.Immutable;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.OpenQuestion;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Modes.OpenQuestion;

/// <summary>
/// The answers of an open question: opened when the game master shows it, typed by the players before the end of the
/// countdown, then locked by the timer, or as soon as every participant answered.
/// </summary>
public sealed class OpenQuestionAnswersTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly ImmutableArray<OpenQuestionRoundDescriptor> _rounds =
        [OpenQuestionGames.Round(OpenQuestionGames.PaintingQuestion, OpenQuestionGames.YearQuestion)];

    private static readonly DateTimeOffset _closeAt = Games.Now.AddSeconds(OpenQuestionRoundDescriptor.DefaultAnswerSeconds);

    [Fact]
    public void Start_FirstQuestion_IsPresentedWithoutAnswers()
    {
        // When
        var round = OpenQuestionGames.RoundOf(Presented());

        // Then
        Assert.Equal((0, OpenQuestionPhase.Presentation, null), (round.QuestionIndex, round.Phase, round.AnswersCloseAt));
        Assert.Empty(round.Participants);
    }

    [Fact]
    public void Handle_ShowQuestion_OpensTheAnswersToThePlayersRegisteredAndStartsTheCountdown()
    {
        // Given
        var state = Presented();

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.ShowQuestion(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = OpenQuestionGames.RoundOf(transition.State);
        Assert.Equal((OpenQuestionPhase.Answering, _closeAt), (round.Phase, round.AnswersCloseAt));
        Assert.Equal([Games.PlayerIdOf(1), Games.PlayerIdOf(2), Games.PlayerIdOf(3)], round.Participants);
        var timer = Assert.IsType<ScheduleTimer>(Assert.Single(transition.Effects));
        Assert.Equal((OpenQuestionMode.AnswersTimer, _closeAt, state.CurrentRound!.Id), (timer.TimerId, timer.DueAt, timer.RoundId));
    }

    [Fact]
    public void Handle_ShowQuestion_QuestionWithItsOwnTime_CountsDownFromIt()
    {
        // Given
        var state = OpenQuestionGames.Started([OpenQuestionGames.Round(OpenQuestionGames.YearQuestion) with { AnswerSeconds = 60 }], _players);

        // When
        var shown = OpenQuestionGames.Answering(state);

        // Then
        Assert.Equal(Games.Now.AddSeconds(15), OpenQuestionGames.RoundOf(shown).AnswersCloseAt);
    }

    [Fact]
    public void Handle_ShowQuestion_Twice_IsRejected() =>
        AssertRejected(OpenQuestionGames.Answering(Presented()), s => OpenQuestionGames.ShowQuestion(s), RejectionReason.PhaseMismatch);

    [Fact]
    public void Handle_ShowQuestion_OtherQuestion_IsRejected() =>
        AssertRejected(Presented(), s => OpenQuestionGames.ShowQuestion(s, questionNumber: 2), RejectionReason.QuestionMismatch);

    [Fact]
    public void Handle_SubmitAnswer_WhileOpen_RecordsItAsTyped()
    {
        // Given
        var state = OpenQuestionGames.Answering(Presented());

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.Answer(state, 2, " Léonard  da VINCI !", Games.Now.AddSeconds(3)), Games.Context());

        // Then: the text typed is kept for the screens, normalized only to be compared
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = OpenQuestionGames.RoundOf(transition.State);
        Assert.Equal(OpenQuestionPhase.Answering, round.Phase);
        Assert.Equal(new OpenAnswer(" Léonard  da VINCI !", Games.Now.AddSeconds(3)), round.Answers[Games.PlayerIdOf(2)]);
    }

    [Fact]
    public void Handle_SubmitAnswer_LastParticipant_LocksTheAnswersAndStopsTheCountdown()
    {
        // Given
        var state = OpenQuestionGames.Answering(Presented(), (1, "Vinci"), (3, "Picasso"));

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.Answer(state, 2, "Raphaël"), Games.Context());

        // Then: kept for the speed bonus
        Assert.Null(transition.Rejection);
        var round = OpenQuestionGames.RoundOf(transition.State);
        Assert.Equal((OpenQuestionPhase.Locked, _closeAt), (round.Phase, round.AnswersCloseAt));
        Assert.Equal(new CancelTimer(OpenQuestionMode.AnswersTimer), Assert.Single(transition.Effects));
    }

    [Fact]
    public void Handle_SubmitAnswer_AtMaxLength_IsAccepted()
    {
        var state = OpenQuestionGames.Answering(Presented());

        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.Answer(state, 1, new string('a', 20)), Games.Context());

        Assert.Null(transition.Rejection);
    }

    [Fact]
    public void Handle_SubmitAnswer_LongerThanMaxLength_IsRejectedRatherThanTruncated() =>
        AssertRejected(OpenQuestionGames.Answering(Presented()), s => OpenQuestionGames.Answer(s, 1, new string('a', 21)), RejectionReason.AnswerTooLong);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("?!")]
    [InlineData("Les")]
    public void Handle_SubmitAnswer_EmptyOnceNormalized_IsRejected(string answer) =>
        AssertRejected(OpenQuestionGames.Answering(Presented()), s => OpenQuestionGames.Answer(s, 1, answer), RejectionReason.AnswerEmpty);

    [Fact]
    public void Handle_SubmitAnswer_Second_IsRejected() =>
        AssertRejected(OpenQuestionGames.Answering(Presented(), (1, "Vinci")), s => OpenQuestionGames.Answer(s, 1, "Michel-Ange"), RejectionReason.AlreadyAnswered);

    [Fact]
    public void Handle_SubmitAnswer_WhileTheQuestionIsRead_RecordsItWithoutLockingTheAnswers()
    {
        // Given: everybody answers but Léa, while the game master reads the question
        var state = OpenQuestionGames.Accepted(Presented(), OpenQuestionGames.Answer(Presented(), 1, "Vinci"));
        state = OpenQuestionGames.Accepted(state, OpenQuestionGames.Answer(state, 2, "Picasso"));

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.Answer(state, 3, "Monet"), Games.Context());

        // Then: the participants are fixed only when the question shows
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = OpenQuestionGames.RoundOf(transition.State);
        Assert.Equal((OpenQuestionPhase.Presentation, 3), (round.Phase, round.Answers.Count));
    }

    [Fact]
    public void Handle_SubmitAnswer_SecondWhileTheQuestionIsRead_IsRejected() =>
        AssertRejected(OpenQuestionGames.Accepted(Presented(), OpenQuestionGames.Answer(Presented(), 1, "Vinci")), s => OpenQuestionGames.Answer(s, 1, "Michel-Ange"), RejectionReason.AlreadyAnswered);

    [Fact]
    public void Handle_ShowQuestion_SomeAnsweredWhileItWasRead_KeepsTheirAnswersAndStartsTheCountdown()
    {
        // Given
        var state = OpenQuestionGames.Accepted(Presented(), OpenQuestionGames.Answer(Presented(), 1, "Vinci"));

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.ShowQuestion(state), Games.Context());

        // Then
        var round = OpenQuestionGames.RoundOf(transition.State);
        Assert.Equal(OpenQuestionPhase.Answering, round.Phase);
        Assert.Equal("Vinci", round.Answers[Games.PlayerIdOf(1)].Text);
        Assert.IsType<ScheduleTimer>(Assert.Single(transition.Effects));
    }

    [Fact]
    public void Handle_ShowQuestion_EverybodyAnsweredWhileItWasRead_LocksTheAnswers()
    {
        // Given
        var state = Presented();
        foreach (var player in new[] { 1, 2, 3 })
        {
            state = OpenQuestionGames.Accepted(state, OpenQuestionGames.Answer(state, player, "Vinci"));
        }

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.ShowQuestion(state), Games.Context());

        // Then: no countdown to wait for
        Assert.Equal(OpenQuestionPhase.Locked, OpenQuestionGames.RoundOf(transition.State).Phase);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Handle_SubmitAnswer_ReceivedAtTheDeadline_IsRejectedEvenBeforeTheTimer() =>
        AssertRejected(OpenQuestionGames.Answering(Presented()), s => OpenQuestionGames.Answer(s, 1, "Vinci", _closeAt), RejectionReason.AnswerTooLate);

    [Fact]
    public void Handle_SubmitAnswer_OnceLocked_IsRejected() =>
        AssertRejected(OpenQuestionGames.Locked(Presented(), (1, "Vinci")), s => OpenQuestionGames.Answer(s, 2, "Vinci"), RejectionReason.PhaseMismatch);

    [Fact]
    public void Handle_SubmitAnswer_OtherQuestion_IsRejected() =>
        AssertRejected(OpenQuestionGames.Answering(Presented()), s => OpenQuestionGames.Answer(s, 1, "1969", questionNumber: 2), RejectionReason.QuestionMismatch);

    [Fact]
    public void Handle_SubmitAnswer_PlayerJoinedOnceTheQuestionShowed_IsRejected()
    {
        // Given
        var state = OpenQuestionGames.Accepted(OpenQuestionGames.Answering(Presented()), Games.Join("Noé", player: 4));

        // When / Then: Noé plays from the next question
        AssertRejected(state, s => OpenQuestionGames.Answer(s, 4, "Vinci"), RejectionReason.NotParticipating);
    }

    [Fact]
    public void Handle_AnswersTimerElapsed_LocksTheAnswers()
    {
        // Given
        var state = OpenQuestionGames.Answering(Presented(), (1, "Vinci"));

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.AnswersTimerElapsed(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(OpenQuestionPhase.Locked, OpenQuestionGames.RoundOf(transition.State).Phase);
    }

    [Fact]
    public void Handle_AnswersTimerElapsed_OnceEverybodyAnswered_IsRejected()
    {
        var state = OpenQuestionGames.Answering(Presented(), (1, "Vinci"), (2, "Vinci"), (3, "Vinci"));

        AssertRejected(state, s => OpenQuestionGames.AnswersTimerElapsed(s, _closeAt), RejectionReason.UnexpectedTimer);
    }

    [Fact]
    public void Handle_AnswersTimerElapsed_DueAtAnotherTime_IsRejected() =>
        AssertRejected(OpenQuestionGames.Answering(Presented()), s => OpenQuestionGames.AnswersTimerElapsed(s, _closeAt.AddSeconds(-1)), RejectionReason.UnexpectedTimer);

    [Fact]
    public void Handle_SkipQuestion_WhileAnswering_PresentsTheNextOneAndStopsTheCountdown()
    {
        // Given
        var state = OpenQuestionGames.Answering(Presented(), (1, "Vinci"));

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.SkipQuestion(state), Games.Context());

        // Then: the answers received are ignored
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Round, transition.State.Phase);
        var round = OpenQuestionGames.RoundOf(transition.State);
        Assert.Equal((1, OpenQuestionPhase.Presentation), (round.QuestionIndex, round.Phase));
        Assert.Empty(round.Answers);
        Assert.Equal([0], round.SkippedQuestions);
        Assert.Equal(new CancelTimer(OpenQuestionMode.AnswersTimer), Assert.Single(transition.Effects));
        Assert.All(transition.State.Players, player => Assert.Equal(0, player.Score));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Handle_SkipQuestion_Presented_OrLocked_PresentsTheNextOne(bool locked)
    {
        var state = locked ? OpenQuestionGames.Locked(Presented()) : Presented();

        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.SkipQuestion(state), Games.Context());

        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(1, OpenQuestionGames.RoundOf(transition.State).QuestionIndex);
    }

    [Fact]
    public void Handle_SkipQuestion_Last_FinishesTheRound()
    {
        // Given
        var state = OpenQuestionGames.Skipped(Presented());

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.SkipQuestion(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase);
    }

    [Fact]
    public void Handle_SkipQuestion_SentTwice_SkipsOnce() =>
        AssertRejected(OpenQuestionGames.Skipped(Presented()), s => OpenQuestionGames.SkipQuestion(s, questionNumber: 1), RejectionReason.QuestionMismatch);

    [Fact]
    public void Handle_IntentOfAnotherMode_IsRejected() =>
        AssertRejected(Presented(), s => new GameMasterRoundInput(new Contracts.Quiz.QuizShowQuestion(s.CurrentRound!.Id, 1), Games.Now), RejectionReason.IntentUnsupported);

    [Fact]
    public void Handle_GameResumedWhileAnswering_GoesOnWithTheTimeLeftAndSchedulesTheCountdownAgain()
    {
        // Given: saved 8 seconds into the countdown, Zoé having answered after 5
        var state = OpenQuestionGames.Answering(Presented());
        state = OpenQuestionGames.Accepted(state, OpenQuestionGames.Answer(state, 1, "Vinci", Games.Now.AddSeconds(5)));
        var resumedAt = Games.Now.AddHours(1);

        // When
        var transition = OpenQuestionGames.Engine.Handle(state, new GameResumed(Games.Now.AddSeconds(8)), new GameContext(resumedAt, new Random(42)));

        // Then
        Assert.Null(transition.Rejection);
        var round = OpenQuestionGames.RoundOf(transition.State);
        var closeAt = resumedAt.AddSeconds(22);
        Assert.Equal((OpenQuestionPhase.Answering, closeAt), (round.Phase, round.AnswersCloseAt));
        Assert.Equal(resumedAt.AddSeconds(-3), round.Answers[Games.PlayerIdOf(1)].ReceivedAt);
        Assert.Equal(new ScheduleTimer(OpenQuestionMode.AnswersTimer, closeAt) { RoundId = state.CurrentRound!.Id }, Assert.Single(transition.Effects));
    }

    [Fact]
    public void Project_WhileAnswering_ShowsEachViewerWhatTheStoryAsks()
    {
        // Given
        var state = OpenQuestionGames.Answering(Presented(), (1, "Vinci"));

        // When
        var display = Assert.IsType<OpenQuestionDisplayView>(OpenQuestionGames.Snapshots.ForDisplay(state).RoundView);
        var gm = Assert.IsType<OpenQuestionGameMasterView>(OpenQuestionGames.Snapshots.ForGameMaster(state).RoundView);
        var zoe = Assert.IsType<OpenQuestionPlayerView>(OpenQuestionGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView);
        var max = Assert.IsType<OpenQuestionPlayerView>(OpenQuestionGames.Snapshots.ForPlayer(state, state.Players[1]).RoundView);

        // Then
        Assert.Equal((OpenQuestionGames.PaintingQuestion.Text, 1, 3, _closeAt.ToUnixTimeMilliseconds()), (display.Text, display.AnsweredCount, display.ParticipantCount, display.AnswersCloseAt));
        Assert.Equal(["Vinci", null, null], gm.Answers.Select(answer => answer.Answer));
        Assert.Equal(("Léonard de Vinci", true), (gm.ExpectedAnswer, gm.QuestionShown));
        Assert.Equal(("Vinci", 20, false), (zoe.Answer, zoe.MaxLength, zoe.Numeric));
        Assert.Null(max.Answer);
    }

    [Fact]
    public void ProjectForDisplay_WhileAnswering_ListsTheParticipantsWithTheTimeTheyTookToAnswer()
    {
        // Given: Zoé answered while the question was read, Max 4.25 s after it showed, Léa not yet
        var presented = Presented();
        var state = OpenQuestionGames.Accepted(presented, OpenQuestionGames.Answer(presented, 1, "Vinci", Games.Now.AddSeconds(-3)));
        state = OpenQuestionGames.Answering(state);
        state = OpenQuestionGames.Accepted(state, OpenQuestionGames.Answer(state, 2, "Picasso", Games.Now.AddMilliseconds(4250)));

        // When
        var view = Assert.IsType<OpenQuestionDisplayView>(OpenQuestionGames.Snapshots.ForDisplay(state).RoundView);

        // Then
        OpenQuestionDisplayParticipant[] expected = [new("Zoé", 0), new("Max", 4250), new("Léa", null)];
        Assert.Equal(expected, view.Participants.ToArray());
    }

    [Fact]
    public void ProjectForDisplay_Presented_ListsNobody()
    {
        var presented = Presented();
        var state = OpenQuestionGames.Accepted(presented, OpenQuestionGames.Answer(presented, 1, "Vinci"));

        var view = Assert.IsType<OpenQuestionDisplayView>(OpenQuestionGames.Snapshots.ForDisplay(state).RoundView);

        Assert.Empty(view.Participants);
    }

    private static GameState Presented() => OpenQuestionGames.Started(_rounds, _players);

    private static void AssertRejected(GameState state, Func<GameState, GameInput> input, RejectionReason reason)
    {
        var transition = OpenQuestionGames.Engine.Handle(state, input(state), Games.Context());

        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }
}
