namespace PartyGame.Contracts;

/// <summary>
/// Where a round of the pack stands in the programme of the game.
/// </summary>
public enum ScheduledRoundStatus
{
    /// <summary>The round was played to its end.</summary>
    Played,

    /// <summary>The game master skipped the round before its end.</summary>
    Skipped,

    /// <summary>The round is announced or in progress, or just finished between two rounds and once the game is finished.</summary>
    Current,

    /// <summary>The round is to be played, in the order of the programme.</summary>
    Upcoming,

    /// <summary>The game master withdrew the round: it is not played unless put back.</summary>
    Withdrawn,
}
