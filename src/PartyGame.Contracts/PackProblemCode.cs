namespace PartyGame.Contracts;

/// <summary>
/// What is wrong in a pack, for the game master to fix it before the game. The client translates each code, with the
/// parameters of the <see cref="PackProblem"/>.
/// </summary>
/// <remarks>
/// Codes prefixed with a game mode are reported by that mode, which checks what the descriptor constraints cannot.
/// </remarks>
public enum PackProblemCode
{
    /// <summary>A question of a quiz round has no correct choice. The path is the one of the question.</summary>
    QuizCorrectChoiceMissing,

    /// <summary>A question of a quiz round has several correct choices. The path is the one of the question.</summary>
    QuizCorrectChoiceDuplicated,

    /// <summary>
    /// A choice of a quiz question repeats an earlier one, ignoring case, accents and spaces. The path is the one of the
    /// repeated choice, and the <c>choice</c> parameter its text.
    /// </summary>
    QuizChoiceDuplicated,
}
