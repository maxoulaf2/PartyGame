using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes.Buzzer;

/// <summary>
/// Plays the rounds of buzzer questions of the packs.
/// </summary>
/// <remarks>
/// For now, the mode only lets the packs be loaded and validated. The questions are played from US-E13-04: until then, a
/// round finishes as soon as it starts, and accepts no intent.
/// </remarks>
public sealed class BuzzerMode : GameMode<BuzzerRoundDescriptor, BuzzerRound>
{
    /// <summary>
    /// Reports nothing: the constraints of the descriptor cover every rule of a buzzer round.
    /// </summary>
    /// <inheritdoc />
    public override ImmutableArray<PackProblem> Validate(BuzzerRoundDescriptor descriptor, string path) => [];

    /// <inheritdoc />
    public override RoundTransition Start(BuzzerRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new BuzzerRound(), []) { IsFinished = true };

    /// <inheritdoc />
    /// <remarks>Never called: the round finishes as soon as it starts, so the engine rejects every input aimed at it.</remarks>
    public override RoundTransition Handle(BuzzerRound round, GameInput input, GameState game, GameContext context) =>
        RoundTransition.Rejected(round, RejectionReason.NotInRound);

    /// <inheritdoc />
    /// <remarks>Never called: a finished round is never resumed.</remarks>
    public override RoundTransition ResumeRound(BuzzerRound round, GameState game, TimeSpan shift, GameContext context) =>
        new(round, []);

    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(BuzzerRound round, GameState game, Player player) => new BuzzerPlayerView();

    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(BuzzerRound round, GameState game) => new BuzzerDisplayView();

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(BuzzerRound round, GameState game) => new BuzzerGameMasterView();
}
