using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// A round of <see cref="FakeMode"/>: what it was started with and every input it got, in order.
/// </summary>
/// <param name="Title">Title of the activity.</param>
/// <param name="CountdownDueAt">When the countdown scheduled at the start elapses.</param>
/// <param name="Inputs">What the round got, starting with its start.</param>
internal sealed record FakeRoundState(string Title, DateTimeOffset CountdownDueAt, ImmutableList<string> Inputs) : RoundState
{
    /// <summary>The image of the activity, which the TV screen shows.</summary>
    public MediaPath? Image { get; init; }
}
