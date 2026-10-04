namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game the server found saved was just resumed. The game loop hands it to the engine right after
/// <see cref="ResumeSavedGame"/>, before any other input, so that the round in progress catches up with the time spent
/// offline: its deadlines move on, and its timers, which are not part of the state, are scheduled again.
/// </summary>
/// <param name="SavedAt">Server time of the last save of the game, before the server stopped.</param>
public sealed record GameResumed(DateTimeOffset SavedAt) : GameInput;
