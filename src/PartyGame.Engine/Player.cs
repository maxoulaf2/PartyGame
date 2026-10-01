using PartyGame.Contracts;

namespace PartyGame.Engine;

/// <summary>
/// A player registered in the game.
/// </summary>
/// <param name="Id">Identifier of the player, independent of their connections.</param>
/// <param name="Nickname">Nickname shown to everybody, already normalized by <see cref="Lobby.NicknameRules"/>.</param>
public sealed record Player(PlayerId Id, string Nickname);
