using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// The reveal of a quiz question: once its answers are locked, the game master shows the correct answer and what each
/// player chose.
/// </summary>
public sealed class QuizRevealTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly ImmutableArray<QuizRoundDescriptor> _rounds =
        [QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.LastQuestion)];

    [Fact]
    public void Handle_RevealAnswer_RevealsTheQuestionAndKeepsItsAnswers()
    {
        // Given
        var state = QuizGames.Locked(Presented(), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.C));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.RevealAnswer(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = QuizGames.RoundOf(transition.State);
        Assert.Equal(QuizPhase.Revealed, round.Phase);
        Assert.Equal(QuizGames.RoundOf(state).Answers, round.Answers);
        Assert.Equal(QuizGames.RoundOf(state).Participants, round.Participants);
    }

    [Fact]
    public void Handle_RevealAnswerLockedByTheTimer_RevealsTheQuestion()
    {
        // Given
        var answering = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.B));
        var state = QuizGames.Accepted(answering, QuizGames.AnswersTimerElapsed(answering));

        // When
        var revealed = QuizGames.Accepted(state, QuizGames.RevealAnswer(state));

        // Then
        Assert.Equal(QuizPhase.Revealed, QuizGames.RoundOf(revealed).Phase);
    }

    [Fact]
    public void Handle_RevealAnswerDuringThePresentation_IsRejected()
    {
        // Given
        var state = Presented();

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.RevealAnswer(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Fact]
    public void Handle_RevealAnswerWhileTheAnswersAreOpen_IsRejected()
    {
        // Given: Max and Léa have not answered yet, and the countdown runs
        var state = QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.RevealAnswer(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Fact]
    public void Handle_RevealAnswerTwice_IsRejectedAsObsolete()
    {
        // Given: two consoles, or a request sent again after a reconnection
        var state = QuizGames.Revealed(Presented(), (1, QuizChoiceLetter.A));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.RevealAnswer(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Handle_RevealAnswerOfAnotherQuestion_IsRejected(int questionNumber)
    {
        // Given
        var state = QuizGames.Locked(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.RevealAnswer(state, questionNumber), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.QuestionMismatch);
    }

    [Fact]
    public void Handle_RevealAnswerOfAnotherRound_IsRejected()
    {
        // Given
        var state = QuizGames.Locked(Presented());
        var reveal = new GameMasterRoundInput(new QuizRevealAnswer(new Contracts.RoundId(Guid.NewGuid()), 1), Games.Now);

        // When
        var transition = QuizGames.Engine.Handle(state, reveal, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.RoundMismatch);
    }

    [Fact]
    public void Handle_SubmitAnswerOnceRevealed_IsRejected()
    {
        // Given
        var state = QuizGames.Revealed(Presented());

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.PhaseMismatch);
    }

    [Fact]
    public void ProjectForDisplay_Revealed_ShowsTheCorrectChoiceAndWhoChoseWhatInOrderOfArrival()
    {
        // Given: Léa answers first, Max does not answer, and Noé joins too late to take part
        var state = QuizGames.Locked(Presented(), (3, QuizChoiceLetter.B), (1, QuizChoiceLetter.A));
        state = QuizGames.Accepted(QuizGames.Accepted(state, QuizGames.RevealAnswer(state)), Games.Join("Noé", player: 4));

        // When
        var view = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;

        // Then
        Assert.Equal(QuizQuestionPhase.Revealed, view.Phase);
        var reveal = Assert.IsType<QuizDisplayReveal>(view.Reveal);
        Assert.Equal(QuizChoiceLetter.A, reveal.CorrectChoice);
        Assert.Equal(
            [
                new QuizRevealedAnswer(Games.PlayerIdOf(1), "Zoé", QuizChoiceLetter.A),
                new QuizRevealedAnswer(Games.PlayerIdOf(2), "Max", null),
                new QuizRevealedAnswer(Games.PlayerIdOf(3), "Léa", QuizChoiceLetter.B),
            ],
            reveal.Answers);
        Assert.Equal((null, 2, 3), (view.AnswersCloseAt, view.AnsweredCount, view.ParticipantCount));
    }

    [Fact]
    public void ProjectForDisplay_RevealedWithShuffledChoices_ShowsTheLetterOfTheCorrectChoiceAsShown()
    {
        // Given: the correct choice, first in the descriptor, is shown under the letter the shuffle gave it
        var shuffled = _rounds[0] with { ShuffleChoices = true };
        var state = QuizGames.Revealed(QuizGames.Started([shuffled], _players));
        var expected = (QuizChoiceLetter)QuizGames.RoundOf(state).ChoiceOrder.IndexOf(0);

        // When
        var view = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;

        // Then
        Assert.NotEqual(QuizChoiceLetter.A, expected);
        Assert.Equal(expected, view.Reveal!.CorrectChoice);
        Assert.Equal("Canberra", view.Choices[(int)expected].Text);
    }

    [Fact]
    public void ProjectForDisplay_RevealedParticipantRenamed_ShowsTheirCurrentNickname()
    {
        // Given
        var state = QuizGames.Accepted(QuizGames.Revealed(Presented(), (2, QuizChoiceLetter.A)), Games.Rename(2, "Maxime"));

        // When
        var view = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;

        // Then
        Assert.Equal("Maxime", view.Reveal!.Answers[1].Nickname);
    }

    [Theory]
    [InlineData(QuizPhase.Presentation)]
    [InlineData(QuizPhase.Answering)]
    [InlineData(QuizPhase.Locked)]
    public void Projections_BeforeTheReveal_HaveNeitherCorrectChoiceNorVerdict(QuizPhase phase)
    {
        // Given
        var state = phase switch
        {
            QuizPhase.Presentation => Presented(),
            QuizPhase.Answering => QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A)),
            _ => QuizGames.Locked(Presented(), (1, QuizChoiceLetter.A)),
        };

        // When
        var display = (QuizDisplayView)QuizGames.Snapshots.ForDisplay(state).RoundView!;
        var player = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView!;

        // Then
        Assert.Null(display.Reveal);
        Assert.Equal((null, null), (player.CorrectChoice, player.Verdict));
    }

    [Fact]
    public void ProjectForPlayer_Revealed_TellsEachParticipantWhetherTheyGotItRight()
    {
        // Given: Zoé is right, Max is wrong, Léa did not answer
        var state = QuizGames.Revealed(Presented(), (1, QuizChoiceLetter.A), (2, QuizChoiceLetter.D));

        // When
        var views = state.Players.Select(p => (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, p).RoundView!).ToArray();

        // Then
        Assert.All(views, view => Assert.Equal((QuizQuestionPhase.Revealed, QuizChoiceLetter.A), (view.Phase, view.CorrectChoice)));
        Assert.Equal(
            [(QuizVerdict.Correct, QuizChoiceLetter.A), (QuizVerdict.Wrong, QuizChoiceLetter.D), (QuizVerdict.NoAnswer, (QuizChoiceLetter?)null)],
            views.Select(view => (view.Verdict!.Value, view.Answer)));
    }

    [Fact]
    public void ProjectForPlayer_RevealedToAPlayerWhoJoinedAfterTheOpening_ShowsTheCorrectChoiceWithoutVerdict()
    {
        // Given
        var state = QuizGames.Accepted(QuizGames.Answering(Presented(), (1, QuizChoiceLetter.A)), Games.Join("Noé", player: 4));
        state = QuizGames.Closed(state);
        state = QuizGames.Accepted(state, QuizGames.RevealAnswer(state));

        // When
        var view = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[3]).RoundView!;

        // Then
        Assert.Equal((false, QuizChoiceLetter.A, null), (view.Participating, view.CorrectChoice, view.Verdict));
    }

    [Fact]
    public void ProjectForGameMaster_Revealed_ShowsTheSameDistribution()
    {
        // Given
        var state = QuizGames.Revealed(Presented(), (1, QuizChoiceLetter.A), (3, QuizChoiceLetter.A));

        // When
        var view = (QuizGameMasterView)QuizGames.Snapshots.ForGameMaster(state).RoundView!;

        // Then
        Assert.Equal(QuizQuestionPhase.Revealed, view.Phase);
        Assert.Equal([2, 0, 0, 0], view.Choices.Select(choice => choice.AnswerCount));
        Assert.Equal([QuizChoiceLetter.A, null, QuizChoiceLetter.A], view.Answers.Select(answer => answer.Choice));
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
