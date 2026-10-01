namespace PartyGame.Engine;

/// <summary>
/// Phase of the game, which decides the inputs the engine accepts.
/// </summary>
public enum GamePhase
{
    /// <summary>Players join and wait for the game master to start the game.</summary>
    Lobby,

    /// <summary>
    /// The game is running. Provisional: E07 replaces it with the rounds of the pack.
    /// </summary>
    Started,
}
