namespace PartyGame.Contracts;

/// <summary>
/// A player as the game master console lists them, to check who is there, to follow their score and to rename them.
/// </summary>
/// <param name="Id">Identifier of the player, which a rename designates. Never their token.</param>
/// <param name="Nickname">Nickname of the player, to show as plain text.</param>
/// <param name="IsConnected">Whether the phone of the player is connected. A disconnected player stays in the list.</param>
/// <param name="Score">The points of the player since the start of the game: 0 until their first points.</param>
/// <param name="ReconnectionCode">
/// The code the player types to join again from another phone or browser, or <see langword="null"/> for a player of a game
/// saved before codes existed. Only the game master sees it.
/// </param>
public sealed record GameMasterPlayer(PlayerId Id, string Nickname, bool IsConnected, int Score, string? ReconnectionCode);
