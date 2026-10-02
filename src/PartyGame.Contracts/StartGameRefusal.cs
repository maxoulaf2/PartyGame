namespace PartyGame.Contracts;

/// <summary>
/// Why the hub refused to start the game. The console shows the state of the snapshot either way.
/// </summary>
public enum StartGameRefusal
{
    /// <summary>Fewer players are registered than <see cref="GameMasterSnapshot.MinimumPlayerCount"/>.</summary>
    NotEnoughPlayers,

    /// <summary>The game is already started, for instance by a double tap or by a second game master.</summary>
    AlreadyStarted,

    /// <summary>The server could not handle the start; the game master may try again.</summary>
    StartFailed,
}
