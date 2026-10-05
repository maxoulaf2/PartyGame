namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// How the server pre-classifies a group of identical answers to an open question, once normalized, for the game master to
/// judge them at a glance.
/// </summary>
public enum OpenQuestionAnswerCategory
{
    /// <summary>The expected answer or one of its variants: checked on the console from the start.</summary>
    Accepted,

    /// <summary>Close to the expected answer or one of its variants, within the tolerance for typos: to check.</summary>
    ToCheck,

    /// <summary>Neither equal nor close to the expected answer or any of its variants.</summary>
    Rejected,
}
