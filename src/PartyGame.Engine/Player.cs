using PartyGame.Contracts;

namespace PartyGame.Engine;

/// <summary>
/// A player registered in the game.
/// </summary>
/// <param name="Id">Identifier of the player, independent of their connections.</param>
/// <param name="Nickname">Nickname shown to everybody, already normalized by <see cref="Lobby.NicknameRules"/>.</param>
/// <param name="IsConnected">
/// Whether at least one connection of the player is open. A player who leaves stays registered, shown as disconnected:
/// nobody is ever excluded from the game.
/// </param>
public sealed record Player(PlayerId Id, string Nickname, bool IsConnected);
