using PartyGame.Contracts;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// What a player does in a round of <see cref="FakeMode"/>, which records it.
/// </summary>
internal sealed record FakePlayerIntent(RoundId RoundId, string Action) : PlayerRoundIntent(RoundId)
{
    /// <summary>An action the round accepts without changing anything.</summary>
    public const string Nothing = "nothing";

    /// <summary>An action the round rejects.</summary>
    public const string Refused = "refused";

    /// <summary>An action worth <see cref="FakeMode.AwardedPoints"/> to its player, the round itself unchanged.</summary>
    public const string Scores = "scores";
}
