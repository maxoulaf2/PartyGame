namespace PartyGame.Contracts;

/// <summary>
/// What went wrong on the server during the game, for the game master alone. The server recovered by itself: the game goes
/// on with the previous state. The console translates each code.
/// </summary>
public enum IncidentCode
{
    /// <summary>
    /// The handling of an input threw, in the engine or in the game mode of the round: the input was ignored and the state
    /// kept as it was.
    /// </summary>
    RoundHandlerFailed,

    /// <summary>
    /// The snapshot of a role could not be projected: the clients of that role keep their previous snapshot until the next
    /// change. The incident names the role, never the content of the projection.
    /// </summary>
    ProjectionFailed,

    /// <summary>
    /// What follows a change of the state failed, such as scheduling a timer: the other effects ran, and the change was
    /// broadcast.
    /// </summary>
    EffectFailed,

    /// <summary>
    /// The TV screen could not render what it had to show, a view of the round in most cases: it shows its waiting screen
    /// instead, and tries again with the next snapshot. The game master may skip the round at once, since the public no
    /// longer sees it.
    /// </summary>
    DisplayViewFailed,

    /// <summary>
    /// The TV screen could not load a media file of the pack, such as the image of a question: it shows the round without
    /// it. The incident names the step of the round that shows it, when the game mode can tell.
    /// </summary>
    DisplayMediaFailed,

    /// <summary>
    /// The game could not be saved to the disk, such as when it is full: the game goes on, but a crash of the server would
    /// lose it. Reported once per series of failures, and forgotten as soon as the game is saved again.
    /// </summary>
    PersistenceFailed,

    /// <summary>
    /// The game saved before the server restarted could not be read, or was saved in a format this server does not know:
    /// it was set aside under another name, and a new game started. The players register again.
    /// </summary>
    SavedGameUnreadable,
}
