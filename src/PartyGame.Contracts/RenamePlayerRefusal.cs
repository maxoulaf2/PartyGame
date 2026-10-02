namespace PartyGame.Contracts;

/// <summary>
/// Why the hub refused a <see cref="RenamePlayerRequest"/>. The console keeps what was typed and shows the reason.
/// </summary>
public enum RenamePlayerRefusal
{
    /// <summary>The nickname is empty, longer than 16 visible characters, or contains control or invisible characters.</summary>
    NicknameInvalid,

    /// <summary>Another player already has this nickname, ignoring case and accents (« Zoé » and « zoe »).</summary>
    NicknameTaken,

    /// <summary>No registered player has this identifier.</summary>
    PlayerUnknown,

    /// <summary>The server could not handle the rename; the game master may try again.</summary>
    RenameFailed,

    /// <summary>The message does not have the expected shape.</summary>
    MessageInvalid,
}
