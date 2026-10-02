namespace PartyGame.Contracts;

/// <summary>
/// Phase of the game, as the snapshots show it to the clients.
/// </summary>
public enum Phase
{
    /// <summary>Players join and wait for the game master to start the game.</summary>
    Lobby,

    /// <summary>A round of the pack is in progress, played by its game mode.</summary>
    Round,

    /// <summary>A round just finished, and the game master has yet to start the next one.</summary>
    BetweenRounds,

    /// <summary>The last round of the pack is over.</summary>
    Finished,
}
