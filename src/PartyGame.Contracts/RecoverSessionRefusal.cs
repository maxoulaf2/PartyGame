namespace PartyGame.Contracts;

/// <summary>
/// Why the hub refused a <see cref="RecoverSessionRequest"/>.
/// </summary>
public enum RecoverSessionRefusal
{
    /// <summary>No player has this reconnection code.</summary>
    CodeUnknown,

    /// <summary>The message does not have the expected shape.</summary>
    MessageInvalid,

    /// <summary>
    /// The server waits for the game master to resume a saved game or start a new one: the phone waits for the next
    /// <see cref="Welcome"/>.
    /// </summary>
    GamePending,
}
