namespace PartyGame.Engine.Effects;

/// <summary>
/// Sets aside the game the server found saved, which the game master chose not to resume, before the new game is saved in
/// its place: a mistake of the game master can still be undone by hand.
/// </summary>
public sealed record ArchiveSavedGame : Effect;
