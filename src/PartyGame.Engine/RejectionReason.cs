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
}
