namespace PartyGame.Contracts;

/// <summary>
/// A player as the TV screen shows them in the lobby.
/// </summary>
/// <param name="Id">Identifier of the player, which lets the TV screen follow them through a rename.</param>
/// <param name="Nickname">Nickname of the player, to show as plain text.</param>
/// <param name="IsConnected">Whether the phone of the player is connected. A disconnected player stays in the list.</param>
public sealed record DisplayPlayer(PlayerId Id, string Nickname, bool IsConnected);
