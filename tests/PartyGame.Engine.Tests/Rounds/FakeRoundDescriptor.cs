using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// An activity played by <see cref="FakeMode"/>.
/// </summary>
internal sealed record FakeRoundDescriptor : RoundDescriptor
{
    /// <summary>
    /// An optional image, shown on the TV screen during the round.
    /// </summary>
    public MediaPath? Image { get; init; }
}
