namespace PartyGame.Contracts;

/// <summary>
/// What the server tells every connection as soon as it is established, before anything else.
/// </summary>
/// <param name="BuildId">
/// The identifier of the client build the server serves, read from its web root at startup, or <see langword="null"/>
/// when it serves none. A page built otherwise is outdated, cached by the browser, and reloads itself.
/// </param>
public sealed record Welcome(string? BuildId);
