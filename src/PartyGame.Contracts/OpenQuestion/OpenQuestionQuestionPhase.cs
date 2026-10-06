namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// Phase of the question in progress in a round of open questions, as the snapshots show it to the clients.
/// </summary>
public enum OpenQuestionQuestionPhase
{
    /// <summary>
    /// The TV screen shows the number of the question, the game master reads it: the players may already answer, without countdown.
    /// </summary>
    Presentation,

    /// <summary>
    /// The question shows on the TV screen and the countdown runs: the players type their answer, until every participant
    /// answered or the countdown ends.
    /// </summary>
    Answering,

    /// <summary>
    /// The answers are locked: no answer is accepted anymore, and the game master judges those received.
    /// </summary>
    Locked,

    /// <summary>
    /// The game master judged the answers, or none was received: the question waits for its reveal.
    /// </summary>
    Judged,

    /// <summary>
    /// The expected answer and every answer received show on the TV screen, and the points of the question are awarded.
    /// </summary>
    Revealed,
}
