using PartyGame.Contracts;
using PartyGame.Contracts.OpenQuestion;

namespace PartyGame.Bots;

/// <summary>
/// How the bots play open questions: they type a guess at random, and the game master judges at random what the server
/// could not accept by itself.
/// </summary>
internal static class OpenQuestionBot
{
    private static readonly string[] _guesses = ["Paris", "Aucune idée", "Napoléon", "Le chat", "Bleu", "Rome", "Mozart"];

    /// <summary>Whether the player can still answer the question in view.</summary>
    public static bool CanAnswer(OpenQuestionPlayerView view) =>
        view is { Participating: true, Answer: null, Phase: OpenQuestionQuestionPhase.Answering };

    /// <summary>A guess at random: a number for a numeric question, never longer than allowed.</summary>
    public static OpenQuestionSubmitAnswer Answer(RoundId roundId, OpenQuestionPlayerView view, Random random)
    {
        var guess = view.Numeric ? random.Next(1, 2100).ToString(System.Globalization.CultureInfo.InvariantCulture) : _guesses[random.Next(_guesses.Length)];
        return new(roundId, view.QuestionNumber, guess[..Math.Min(guess.Length, view.MaxLength)]);
    }

    /// <summary>Whether the snapshot shows the answer recorded.</summary>
    public static bool Reflects(PlayerSnapshot snapshot, OpenQuestionSubmitAnswer answer) =>
        snapshot.Round?.RoundId == answer.RoundId
        && snapshot.RoundView is OpenQuestionPlayerView view
        && view.QuestionNumber == answer.QuestionNumber
        && view.Answer == answer.Answer;

    /// <summary>
    /// What the game master does next: the question, the judgment once the answers are locked (the answers the server
    /// accepts, and a third of the others at random), the reveal, then the next question. Null while the answers are open.
    /// </summary>
    public static GameMasterRoundIntent? NextStep(RoundId roundId, OpenQuestionGameMasterView view, Random random) => view.Phase switch
    {
        OpenQuestionQuestionPhase.Presentation => new OpenQuestionShowQuestion(roundId, view.QuestionNumber),
        OpenQuestionQuestionPhase.Locked => new OpenQuestionJudge(
            roundId,
            view.QuestionNumber,
            [.. view.Groups.Where(group => group.Category == OpenQuestionAnswerCategory.Accepted || random.Next(3) == 0).SelectMany(group => group.PlayerIds)]),
        OpenQuestionQuestionPhase.Judged => new OpenQuestionRevealAnswer(roundId, view.QuestionNumber),
        OpenQuestionQuestionPhase.Revealed => new OpenQuestionNextQuestion(roundId, view.QuestionNumber),
        _ => null,
    };
}
