namespace PartyGame.Contracts;

/// <summary>
/// Why the hub refused a <see cref="JoinRequest"/>. The phone stays on the registration screen and keeps what was typed.
/// </summary>
public enum JoinRefusal
{
    /// <summary>The nickname is empty, longer than 16 visible characters, or contains control or invisible characters.</summary>
    NicknameInvalid,

    /// <summary>Another player already has this nickname, ignoring case and accents (« Zoé » and « zoe »).</summary>
    NicknameTaken,

    /// <summary>This connection already registered a player.</summary>
    AlreadyJoined,

    /// <summary>The server could not handle the registration; the phone may try again.</summary>
    JoinFailed,

    /// <summary>The message does not have the expected shape.</summary>
    MessageInvalid,

    /// <summary>
    /// The server waits for the game master to resume a saved game or start a new one: the phone waits for the next
    /// <see cref="Welcome"/>.
    /// </summary>
    GamePending,
}
