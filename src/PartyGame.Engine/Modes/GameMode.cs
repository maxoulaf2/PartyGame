using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes;

/// <summary>
/// Base of the game modes, typed by the descriptor of their activities and by the state of their rounds, so that a mode
/// never casts what the engine hands it.
/// </summary>
/// <typeparam name="TDescriptor">The descriptor of the activities the mode plays.</typeparam>
/// <typeparam name="TState">The state of a round of the mode.</typeparam>
public abstract class GameMode<TDescriptor, TState> : IGameMode
    where TDescriptor : RoundDescriptor
    where TState : RoundState
{
    /// <inheritdoc />
    public Type DescriptorType => typeof(TDescriptor);

    /// <inheritdoc />
    public Type StateType => typeof(TState);

    /// <inheritdoc cref="IGameMode.Validate" />
    public abstract ImmutableArray<PackProblem> Validate(TDescriptor descriptor, string path);

    /// <inheritdoc cref="IGameMode.Start" />
    public abstract RoundTransition Start(TDescriptor descriptor, GameState game, GameContext context);

    /// <inheritdoc cref="IGameMode.Handle" />
    public abstract RoundTransition Handle(TState round, GameInput input, GameState game, GameContext context);

    /// <summary>
    /// Abstract: each mode decides how its rounds resume, since only it knows their deadlines and timers.
    /// </summary>
    /// <inheritdoc cref="IGameMode.ResumeRound" />
    public abstract RoundTransition ResumeRound(TState round, GameState game, TimeSpan shift, GameContext context);

    /// <inheritdoc cref="IGameMode.ProjectForPlayer" />
    public abstract PlayerRoundView ProjectForPlayer(TState round, GameState game, Player player);

    /// <inheritdoc cref="IGameMode.ProjectForDisplay" />
    public abstract DisplayRoundView ProjectForDisplay(TState round, GameState game);

    /// <inheritdoc cref="IGameMode.ProjectForGameMaster" />
    public abstract GameMasterRoundView ProjectForGameMaster(TState round, GameState game);

    /// <summary>
    /// Tells no step unless overridden: the incident then names the round alone.
    /// </summary>
    /// <inheritdoc cref="IGameMode.LocateMedia" />
    public virtual int? LocateMedia(TState round, MediaPath media) => null;

    /// <summary>
    /// Tells no step unless overridden: the game master then sees the round alone.
    /// </summary>
    /// <inheritdoc cref="IGameMode.StepOf" />
    public virtual RoundStep? StepOf(TState round) => null;

    /// <inheritdoc cref="IGameMode.CountPreviewSteps" />
    public abstract int CountPreviewSteps(TDescriptor descriptor);

    /// <inheritdoc cref="IGameMode.Preview" />
    public abstract RoundPreview Preview(TDescriptor descriptor, int stepIndex, DateTimeOffset? excerptStartsAt);

    ImmutableArray<PackProblem> IGameMode.Validate(RoundDescriptor descriptor, string path) =>
        Validate((TDescriptor)descriptor, path);

    RoundTransition IGameMode.Start(RoundDescriptor descriptor, GameState game, GameContext context) =>
        Start((TDescriptor)descriptor, game, context);

    RoundTransition IGameMode.Handle(RoundState round, GameInput input, GameState game, GameContext context) =>
        Handle((TState)round, input, game, context);

    RoundTransition IGameMode.ResumeRound(RoundState round, GameState game, TimeSpan shift, GameContext context) =>
        ResumeRound((TState)round, game, shift, context);

    PlayerRoundView IGameMode.ProjectForPlayer(RoundState round, GameState game, Player player) =>
        ProjectForPlayer((TState)round, game, player);

    DisplayRoundView IGameMode.ProjectForDisplay(RoundState round, GameState game) => ProjectForDisplay((TState)round, game);

    GameMasterRoundView IGameMode.ProjectForGameMaster(RoundState round, GameState game) =>
        ProjectForGameMaster((TState)round, game);

    int? IGameMode.LocateMedia(RoundState round, MediaPath media) => LocateMedia((TState)round, media);

    RoundStep? IGameMode.StepOf(RoundState round) => StepOf((TState)round);

    int IGameMode.CountPreviewSteps(RoundDescriptor descriptor) => CountPreviewSteps((TDescriptor)descriptor);

    RoundPreview IGameMode.Preview(RoundDescriptor descriptor, int stepIndex, DateTimeOffset? excerptStartsAt) =>
        Preview((TDescriptor)descriptor, stepIndex, excerptStartsAt);
}
