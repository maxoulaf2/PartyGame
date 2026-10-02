using PartyGame.Contracts;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// What the game master does in a round of <see cref="FakeMode"/>: finish it, change nothing, or anything else, recorded.
/// </summary>
internal sealed record FakeGameMasterIntent(RoundId RoundId, string Action) : GameMasterRoundIntent(RoundId)
{
    public const string Finish = "finish";

    public const string Nothing = "nothing";
}
