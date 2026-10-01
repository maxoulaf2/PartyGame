namespace PartyGame.Contracts;

/// <summary>
/// Why the hub refused an <see cref="Announcement"/>. The connection then has no role and belongs to no group.
/// </summary>
public enum AnnouncementRefusal
{
    /// <summary>The game master code is missing or wrong, for instance because the server restarted with a new one.</summary>
    GameMasterCodeInvalid,

    /// <summary>The message does not have the expected shape: a missing field, an unknown or unexpected role.</summary>
    MessageInvalid,
}
