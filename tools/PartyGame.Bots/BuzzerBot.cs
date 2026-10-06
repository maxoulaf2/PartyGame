using PartyGame.Contracts;
using PartyGame.Contracts.Buzzer;

namespace PartyGame.Bots;

/// <summary>
/// How the bots play buzzer questions: they press after a delay their behavior tells, and the game master judges each
/// answer at random, then reveals the answer when nobody buzzes.
/// </summary>
internal static class BuzzerBot
{
    /// <summary>
    /// When the bot presses a buzzer that opened at <paramref name="openedAt"/>, in server time (milliseconds), or null when
    /// it never presses. The blind test buzzes the same way.
    /// </summary>
    public static long? BuzzAt(long openedAt, BotBehavior behavior, Random random) => behavior switch
    {
        BotBehavior.Fast => openedAt,
        BotBehavior.Slow => openedAt + random.NextInt64(4000, 6000),
        BotBehavior.Random or BotBehavior.Flaky => openedAt + random.NextInt64(500, 4000),
        _ => null,
    };

    /// <summary>Whether the button shows the press recorded, whoever got the hand.</summary>
    public static bool Pressed(BuzzerButtonState state) =>
        state is BuzzerButtonState.Buzzed or BuzzerButtonState.Won or BuzzerButtonState.Lost;

    /// <summary>Whether the snapshot shows the buzz recorded.</summary>
    public static bool Reflects(PlayerSnapshot snapshot, BuzzerBuzz buzz) =>
        snapshot.Round?.RoundId == buzz.RoundId
        && snapshot.RoundView is BuzzerPlayerView view
        && view.QuestionNumber == buzz.QuestionNumber
        && view.Opening == buzz.Opening
        && Pressed(view.Buzzer);

    /// <summary>
    /// What the game master does next: asks the question, judges the player who has the hand at random, reveals the answer
    /// once nobody may buzz or nobody did for a while (<paramref name="stalled"/>), then moves to the next question.
    /// </summary>
    public static GameMasterRoundIntent? NextStep(RoundId roundId, BuzzerGameMasterView view, bool stalled, Random random) => view.Phase switch
    {
        BuzzerQuestionPhase.Ready => new BuzzerAskQuestion(roundId, view.QuestionNumber, ShowQuestion: true),
        BuzzerQuestionPhase.Open when stalled => new BuzzerRevealAnswer(roundId, view.QuestionNumber),
        BuzzerQuestionPhase.Answering => new BuzzerJudge(roundId, view.QuestionNumber, view.Opening, Correct: random.Next(2) == 0),
        BuzzerQuestionPhase.Closed => new BuzzerRevealAnswer(roundId, view.QuestionNumber),
        BuzzerQuestionPhase.Revealed => new BuzzerNextQuestion(roundId, view.QuestionNumber),
        _ => null,
    };
}
