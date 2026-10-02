using System.Collections.Immutable;
using PartyGame.Contracts;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// What a round of <see cref="FakeMode"/> shows on the game master console: everything it got.
/// </summary>
internal sealed record FakeGameMasterView(string Title, ImmutableArray<string> Inputs) : GameMasterRoundView;
