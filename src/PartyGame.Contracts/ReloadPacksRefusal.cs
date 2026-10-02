namespace PartyGame.Contracts;

/// <summary>
/// Why the hub refused to reload the packs. The console shows the catalog of the snapshot either way.
/// </summary>
public enum ReloadPacksRefusal
{
    /// <summary>The game is started: its pack is fixed, and the files of the packs are not read anymore.</summary>
    AlreadyStarted,

    /// <summary>The server could not handle the reload; the game master may try again.</summary>
    ReloadFailed,
}
