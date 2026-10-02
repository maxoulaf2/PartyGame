using PartyGame.Contracts;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// What a round of <see cref="FakeMode"/> shows on the phone of one player.
/// </summary>
internal sealed record FakePlayerView(string Nickname, int InputCount) : PlayerRoundView;
