using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// Answers a phone sends again, with the same number, because it lost its connection before the acknowledgment: they
/// count once, and only for the question they name.
/// </summary>
public sealed class QuizResendTests
{
    private static readonly string[] _players = ["Zoé", "Max"];

    private static readonly ImmutableArray<QuizRoundDescriptor> _rounds =
        [QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.IllustratedQuestion, QuizGames.LastQuestion)];

    [Fact]
    public void Handle_SubmitAnswerSentAgainOnceHandled_IsRejectedAsAlreadyHandled()
    {
        // Given: Zoé's answer was handled, and the acknowledgment lost
        var opened = QuizGames.Answering(Presented());
        var answer = QuizGames.Answer(opened, 1, QuizChoiceLetter.B);
        var state = QuizGames.Accepted(opened, answer);

        // When: her phone sends it again once reconnected
        var transition = QuizGames.Engine.Handle(state, answer with { ReceivedAt = Games.Now.AddSeconds(4) }, Games.Context());

        // Then: rejected before the mode, which would have answered AlreadyAnswered
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.IntentAlreadyHandled, transition.Rejection);
    }

    [Fact]
    public void Handle_SubmitAnswerSentAgainWhileTheAnswersAreStillOpen_CountsWhenItWasReceived()
    {
        // Given: the answer of Zoé never reached the server
        var state = QuizGames.Answering(Presented());
        var lost = QuizGames.Answer(state, 1, QuizChoiceLetter.B);
        var receivedAt = Games.Now.AddSeconds(6);

        // When: her phone sends it again once reconnected, before the countdown ends
        var transition = QuizGames.Engine.Handle(state, lost with { ReceivedAt = receivedAt }, Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var answer = Assert.Single(QuizGames.RoundOf(transition.State).Answers);
        Assert.Equal((Games.PlayerIdOf(1), new QuizAnswer(QuizChoiceLetter.B, receivedAt)), (answer.Key, answer.Value));
        Assert.Equal(lost.ClientSeq, transition.State.Players[0].LastClientSeq);
    }

    [Fact]
    public void Handle_SubmitAnswerSentAgainOnceTheNextQuestionIsOpen_CountsForNoQuestion()
    {
        // Given: the answer of Zoé to the first question never reached the server, and the second question is open
        var first = QuizGames.Answering(Presented());
        var lost = QuizGames.Answer(first, 1, QuizChoiceLetter.B);
        var revealed = QuizGames.Accepted(QuizGames.Closed(first), QuizGames.RevealAnswer(first));
        var state = QuizGames.Answering(QuizGames.Accepted(revealed, QuizGames.NextQuestion(revealed)));

        // When: her phone sends it again once reconnected
        var transition = QuizGames.Engine.Handle(state, lost with { ReceivedAt = Games.Now.AddSeconds(2) }, Games.Context());

        // Then: rejected by the mode, and her next answer still counts
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
        var answered = QuizGames.Accepted(state, QuizGames.Answer(state, 1, QuizChoiceLetter.A) with { ClientSeq = lost.ClientSeq + 1 });
        Assert.Equal(QuizChoiceLetter.A, QuizGames.RoundOf(answered).Answers[Games.PlayerIdOf(1)].Choice);
    }

    private static GameState Presented() => QuizGames.Started(_rounds, _players);
}
