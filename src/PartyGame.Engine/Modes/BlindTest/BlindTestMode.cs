using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes.BlindTest;

/// <summary>
/// Plays the blind test rounds of the packs.
/// </summary>
/// <remarks>
/// For now, the mode only lets the packs be loaded and validated. The tracks are played from US-E15-02: until then, a
/// round finishes as soon as it starts, and accepts no intent.
/// </remarks>
public sealed class BlindTestMode : GameMode<BlindTestRoundDescriptor, BlindTestRound>
{
    /// <summary>
    /// Checks that at least one track earns points: otherwise the round could not tell the players apart.
    /// </summary>
    /// <inheritdoc />
    public override ImmutableArray<PackProblem> Validate(BlindTestRoundDescriptor descriptor, string path)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var earnsPoints = descriptor.TitlePoints > 0
            || (descriptor.ArtistPoints > 0 && descriptor.Tracks.Any(track => track.Artist is not null));
        return earnsPoints
            ? []
            : [new PackProblem(PackProblemCode.BlindTestPointsMissing, PackDescriptor.FileName, path, ImmutableDictionary<string, string>.Empty)];
    }

    /// <inheritdoc />
    public override RoundTransition Start(BlindTestRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new BlindTestRound(), []) { IsFinished = true };

    /// <inheritdoc />
    /// <remarks>Never called: the round finishes as soon as it starts, so the engine rejects every input aimed at it.</remarks>
    public override RoundTransition Handle(BlindTestRound round, GameInput input, GameState game, GameContext context) =>
        RoundTransition.Rejected(round, RejectionReason.NotInRound);

    /// <inheritdoc />
    /// <remarks>Never called: a finished round is never resumed.</remarks>
    public override RoundTransition ResumeRound(BlindTestRound round, GameState game, TimeSpan shift, GameContext context) =>
        new(round, []);

    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(BlindTestRound round, GameState game, Player player) => new BlindTestPlayerView();

    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(BlindTestRound round, GameState game) => new BlindTestDisplayView();

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(BlindTestRound round, GameState game) => new BlindTestGameMasterView();
}
