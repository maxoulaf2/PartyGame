using PartyGame.Contracts;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// What the game master does in a round of <see cref="FakeMode"/>: finish it, change nothing, award points, or anything
/// else, recorded.
/// </summary>
internal sealed record FakeGameMasterIntent(RoundId RoundId, string Action) : GameMasterRoundIntent(RoundId)
{
    public const string Finish = "finish";

    /// <summary>Awards <see cref="FakeMode.AwardedPoints"/> to every player, the round itself unchanged.</summary>
    public const string Award = "award";

    public const string Nothing = "nothing";
}
