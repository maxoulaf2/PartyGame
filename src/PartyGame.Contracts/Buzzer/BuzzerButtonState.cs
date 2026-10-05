namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// What the buzzer of one phone shows in a round of buzzer questions.
/// </summary>
public enum BuzzerButtonState
{
    /// <summary>The question is not asked yet: nothing to buzz on.</summary>
    Closed,

    /// <summary>The buzzer is open: the player may buzz.</summary>
    Open,

    /// <summary>The player buzzed, and the arbitration window has yet to designate the winner.</summary>
    Buzzed,

    /// <summary>The player has the hand: they answer out loud.</summary>
    Won,

    /// <summary>Another player has the hand.</summary>
    Lost,

    /// <summary>The player may not buzz on this question anymore.</summary>
    Blocked,
}
