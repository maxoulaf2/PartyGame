namespace PartyGame.Contracts;

/// <summary>
/// Role of a client, which decides the snapshot projection it receives.
/// </summary>
public enum Role
{
    /// <summary>A player, on their phone.</summary>
    Player,

    /// <summary>The TV screen everybody watches.</summary>
    Display,

    /// <summary>The game master, who runs the game.</summary>
    GameMaster,
}
