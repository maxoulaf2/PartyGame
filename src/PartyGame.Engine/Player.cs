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
/// <param name="LastClientSeq">
/// The number of the last intent of the player the engine accepted, 0 before the first one: an intent numbered up to it
/// was already handled, and is sent again. Persisted with the state, so that a resumed game still ignores them.
/// </param>
/// <param name="Score">
/// The points of the player since the start of the game, whatever the rounds that awarded them. A player who joins
/// during the game starts at 0.
/// </param>
/// <param name="JoinedAfterEnd">
/// Whether the player joined once the game was finished: having played no round, they are not ranked.
/// </param>
/// <param name="PreviousRank">
/// The rank of the player in the ranking shown after the round before the current one, so that the next ranking shows
/// who gained or lost places; <see langword="null"/> during the first round and for a player who joined since.
/// </param>
public sealed record Player(
    PlayerId Id,
    string Nickname,
    bool IsConnected,
    long LastClientSeq = 0,
    int Score = 0,
    bool JoinedAfterEnd = false,
    int? PreviousRank = null);
