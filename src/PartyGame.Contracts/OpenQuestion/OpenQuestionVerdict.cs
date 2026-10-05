namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// What the reveal of an open question tells a player who took part in it.
/// </summary>
public enum OpenQuestionVerdict
{
    /// <summary>The game master accepted the answer of the player.</summary>
    Correct,

    /// <summary>The game master refused the answer of the player.</summary>
    Wrong,

    /// <summary>The player took part but did not answer before the answers were locked.</summary>
    NoAnswer,
}
