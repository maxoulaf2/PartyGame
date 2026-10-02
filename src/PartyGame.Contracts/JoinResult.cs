namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub to a <see cref="JoinRequest"/>: the identity of the new player, or why there is none. The only
/// message that ever carries a token, sent to the phone that registered and to nobody else.
/// </summary>
/// <param name="Refusal">Why the registration was refused, or <see langword="null"/> when the player is registered.</param>
/// <param name="PlayerId">Identifier of the new player, or <see langword="null"/> when refused.</param>
/// <param name="Token">
/// Secret the phone keeps to be recognized after a reconnection, or <see langword="null"/> when refused. Never part of
/// a snapshot.
/// </param>
public sealed record JoinResult(JoinRefusal? Refusal, PlayerId? PlayerId, string? Token);
