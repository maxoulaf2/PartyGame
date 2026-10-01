namespace PartyGame.Contracts;

/// <summary>
/// Phase of the game, as the snapshots show it to the clients.
/// </summary>
public enum Phase
{
    /// <summary>Players join and wait for the game master to start the game.</summary>
    Lobby,

    /// <summary>
    /// The game is running. Provisional: the rounds of the pack replace it (E07).
    /// </summary>
    Started,
}
