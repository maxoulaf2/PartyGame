using PartyGame.Contracts;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// What a round of <see cref="FakeMode"/> shows on the TV screen.
/// </summary>
internal sealed record FakeDisplayView(string Title, int InputCount) : DisplayRoundView;
