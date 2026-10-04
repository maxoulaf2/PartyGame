namespace PartyGame.Contracts;

/// <summary>
/// What the server tells every connection as soon as it is established, before anything else, and again to every
/// connection once the game master resolved the game found saved.
/// </summary>
/// <param name="BuildId">
/// The identifier of the client build the server serves, read from its web root at startup, or <see langword="null"/>
/// when it serves none. A page built otherwise is outdated, cached by the browser, and reloads itself.
/// </param>
/// <param name="GamePending">
/// Whether the server waits for the game master to resume a saved game or start a new one: a phone then neither registers
/// nor resumes its session, and waits for the next welcome.
/// </param>
public sealed record Welcome(string? BuildId, bool GamePending);
