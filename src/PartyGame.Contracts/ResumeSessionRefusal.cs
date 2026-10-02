namespace PartyGame.Contracts;

/// <summary>
/// Why the hub refused a <see cref="ResumeSessionRequest"/>.
/// </summary>
public enum ResumeSessionRefusal
{
    /// <summary>
    /// The server knows no player with this token, for instance because it restarted or the token comes from another
    /// evening. The phone forgets the token and registers again.
    /// </summary>
    SessionUnknown,

    /// <summary>This connection already registered or identified a player.</summary>
    AlreadyIdentified,

    /// <summary>The server could not resume the session; the phone may try again.</summary>
    ResumeFailed,

    /// <summary>The message does not have the expected shape.</summary>
    MessageInvalid,
}
