using PartyGame.Contracts;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// What a round of <see cref="FakeMode"/> shows on the TV screen.
/// </summary>
/// <param name="Title">The title of the round.</param>
/// <param name="InputCount">The number of inputs the round got.</param>
/// <param name="ImageUrl">The URL of the image of the round, if it has one.</param>
internal sealed record FakeDisplayView(string Title, int InputCount, string? ImageUrl = null) : DisplayRoundView;
