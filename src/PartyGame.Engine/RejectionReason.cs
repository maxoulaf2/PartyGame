namespace PartyGame.Engine;

/// <summary>
/// Why the engine rejected an input. A rejection is a normal case, not an error: the state stays the same.
/// </summary>
public enum RejectionReason
{
    /// <summary>A timer elapsed that the current state no longer waits for.</summary>
    UnexpectedTimer,

    /// <summary>The player identifier or token is already registered, for instance when a registration is replayed.</summary>
    PlayerAlreadyJoined,

    /// <summary>The nickname is empty, too long, or contains control or invisible characters.</summary>
    NicknameInvalid,

    /// <summary>Another player already has this nickname, ignoring case and accents.</summary>
    NicknameTaken,

    /// <summary>The input concerns a player who is not registered.</summary>
    PlayerUnknown,

    /// <summary>The player is already shown as disconnected.</summary>
    PlayerAlreadyDisconnected,

    /// <summary>The player is already shown as connected.</summary>
    PlayerAlreadyConnected,

    /// <summary>The game cannot start with fewer players than the minimum.</summary>
    NotEnoughPlayers,

    /// <summary>
    /// The game is already started: a start, a choice of pack and a reload of the packs are only allowed from the lobby.
    /// </summary>
    GameAlreadyStarted,

    /// <summary>The catalog has no pack with this identifier, for instance one removed by a reload meanwhile.</summary>
    PackUnknown,

    /// <summary>The pack has problems: it cannot be chosen until they are fixed and the packs reloaded.</summary>
    PackInvalid,

    /// <summary>The game cannot start before the game master chooses a pack.</summary>
    PackNotSelected,

    /// <summary>The address is not one of the candidates the server offers to the game master.</summary>
    AddressUnknown,

    /// <summary>An activity of the pack has no registered game mode to play it: the game cannot start.</summary>
    GameModeMissing,

    /// <summary>An intent aimed at a round arrived while no round is in progress.</summary>
    NotInRound,

    /// <summary>
    /// The input names another round than the one in progress, or than the one that just finished: it is obsolete, for
    /// instance sent twice or by a second game master console, or aimed at the wrong round.
    /// </summary>
    RoundMismatch,

    /// <summary>The next round is asked for while the game is not between two rounds.</summary>
    NotBetweenRounds,
}
