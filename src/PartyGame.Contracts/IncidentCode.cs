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
}
