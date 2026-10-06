using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;

namespace PartyGame.Bots;

/// <summary>
/// How the bots play the quiz: from the projections a phone and a console get, never from anything more.
/// </summary>
internal static class QuizBot
{
    /// <summary>A slow bot answers this long before the deadline, enough for the message to arrive in time.</summary>
    private const long SlowMarginMs = 1000;

    /// <summary>Whether the player can still answer the question in view.</summary>
    public static bool CanAnswer(QuizPlayerView view) =>
        view is { Participating: true, Answer: null, Phase: QuizQuestionPhase.Presentation or QuizQuestionPhase.Answering, ShownChoiceCount: > 0 };

    /// <summary>
    /// When the bot answers a question that closes at <paramref name="answersCloseAt"/>, in server time (milliseconds), or
    /// null when it does not know yet or never answers. Open questions are answered the same way.
    /// </summary>
    public static long? AnswerAt(long? answersCloseAt, BotBehavior behavior, long now, Random random) => behavior switch
    {
        BotBehavior.Fast => now,

        // A countdown starts once the last choice shows: the slow bot waits for it.
        BotBehavior.Slow => answersCloseAt - SlowMarginMs,
        BotBehavior.Random or BotBehavior.Flaky => Math.Min(now + random.NextInt64(500, 4000), (answersCloseAt ?? long.MaxValue) - SlowMarginMs),
        _ => null,
    };

    /// <summary>The answer of the bot, among the choices shown now: the first ones of the question.</summary>
    public static QuizSubmitAnswer Answer(RoundId roundId, QuizPlayerView view, BotBehavior behavior, Random random) =>
        new(roundId, view.QuestionNumber, behavior == BotBehavior.Fast ? view.Choices[0] : view.Choices[random.Next(view.ShownChoiceCount)]);

    /// <summary>Whether the snapshot shows the answer recorded.</summary>
    public static bool Reflects(PlayerSnapshot snapshot, QuizSubmitAnswer answer) =>
        snapshot.Round?.RoundId == answer.RoundId
        && snapshot.RoundView is QuizPlayerView view
        && view.QuestionNumber == answer.QuestionNumber
        && view.Answer == answer.Choice;

    /// <summary>
    /// What the game master does next, as the console offers it: the question, each choice in turn, the reveal once the
    /// answers are locked, then the next question. Null while the answers are open.
    /// </summary>
    public static GameMasterRoundIntent? NextStep(RoundId roundId, QuizGameMasterView view) => view.Phase switch
    {
        QuizQuestionPhase.Presentation when !view.QuestionShown => new QuizShowQuestion(roundId, view.QuestionNumber),
        QuizQuestionPhase.Presentation => new QuizShowChoice(roundId, view.QuestionNumber, (QuizChoiceLetter)view.Choices.Count(choice => choice.Shown)),
        QuizQuestionPhase.Locked => new QuizRevealAnswer(roundId, view.QuestionNumber),
        QuizQuestionPhase.Revealed => new QuizNextQuestion(roundId, view.QuestionNumber),
        _ => null,
    };
}
