using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// A phone asks to join the game. The hub generates the identifier and the token before handing it to the engine.
/// </summary>
/// <param name="PlayerId">Identifier the player gets if the registration is accepted.</param>
/// <param name="Token">Token the player gets if the registration is accepted.</param>
/// <param name="Nickname">Nickname as typed on the phone, normalized by the engine.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record JoinGame(PlayerId PlayerId, PlayerToken Token, string Nickname, DateTimeOffset ReceivedAt)
    : Intent(ReceivedAt);
