using PartyGame.Contracts;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// What a player does in a round of <see cref="FakeMode"/>, which records it.
/// </summary>
internal sealed record FakePlayerIntent(RoundId RoundId, string Action) : PlayerRoundIntent(RoundId);
