namespace PartyGame.Contracts;

/// <summary>
/// The game master resumes the game a restarted server found saved, or starts a new one instead.
/// </summary>
/// <param name="SavedGameId">
/// The game found, as <see cref="GameMasterSavedGame.GameId"/> names it. A request that names another game, or comes once
/// the decision is made (double tap, second console), is obsolete and changes nothing.
/// </param>
/// <param name="Resume">
/// Whether to resume the game found; otherwise, a new game starts in the lobby, the players registering again.
/// </param>
public sealed record ResolveSavedGameRequest(GameId SavedGameId, bool Resume);
