namespace PartyGame.Contracts;

/// <summary>
/// How an error of a page came to light, as reported in a <see cref="ClientErrorReport"/>.
/// </summary>
public enum ClientErrorKind
{
    /// <summary>An exception nothing caught, such as one thrown by an event handler.</summary>
    Error,

    /// <summary>A promise rejected without any handler.</summary>
    UnhandledRejection,

    /// <summary>A view whose rendering threw, replaced by the neutral waiting screen.</summary>
    RenderFailed,
}
