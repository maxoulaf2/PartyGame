namespace PartyGame.Engine;

/// <summary>
/// Phase of the game, which decides the inputs the engine accepts.
/// </summary>
public enum GamePhase
{
    /// <summary>Players join and wait for the game master to start the game.</summary>
    Lobby,

    /// <summary>A round is in progress: the engine hands the inputs aimed at it to its game mode.</summary>
    Round,

    /// <summary>A round just finished, and the game master has yet to ask for the next one.</summary>
    BetweenRounds,

    /// <summary>The last round is over.</summary>
    Finished,

    /// <summary>
    /// The server restarted and found a saved game, kept in <see cref="GameState.PendingGame"/>: it waits for the game
    /// master to resume it or start a new one. The engine accepts nothing else meanwhile.
    /// </summary>
    ResumePending,
}
