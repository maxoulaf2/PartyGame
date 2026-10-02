namespace PartyGame.Contracts;

/// <summary>
/// Why the hub refused a <see cref="SelectPackRequest"/>. The console shows the selection of the snapshot either way.
/// </summary>
public enum SelectPackRefusal
{
    /// <summary>No pack of the catalog has this identifier, for instance one removed by a reload meanwhile.</summary>
    PackUnknown,

    /// <summary>The pack has problems: it cannot be played until they are fixed.</summary>
    PackInvalid,

    /// <summary>The game is started: its pack cannot change anymore.</summary>
    AlreadyStarted,

    /// <summary>The server could not handle the choice; the game master may try again.</summary>
    SelectionFailed,

    /// <summary>The message does not have the expected shape.</summary>
    MessageInvalid,
}
