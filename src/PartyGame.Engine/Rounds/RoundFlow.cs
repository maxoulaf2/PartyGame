using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;

namespace PartyGame.Engine.Rounds;

/// <summary>
/// Sequence of the rounds of the pack: each one is started, fed with the inputs aimed at it and ended by its game mode,
/// then the game master asks for the next one, until the last one.
/// </summary>
internal static class RoundFlow
{
    /// <summary>
    /// Starts a round of the pack. Every activity has a game mode: the start of the game checked it.
    /// </summary>
    public static Transition Start(GameState state, int index, GameModes modes, GameContext context)
    {
        var descriptor = state.Rounds[index];
        var started = modes.For(descriptor).Start(descriptor, state, context);
        if (started.Rejection is { } rejection)
        {
            throw new InvalidOperationException($"Game mode for {descriptor.GetType().Name} rejected the start of a round: {rejection}.");
        }

        var round = new PlayedRound(NewRoundId(context.Random), index, started.State);
        return Apply(state with { Phase = GamePhase.Round, CurrentRound = round }, round, started);
    }

    /// <summary>
    /// Starts the round that follows the one the game master names, which must be the one that just finished: a request
    /// sent twice, or by two consoles at once, starts it only once.
    /// </summary>
    public static Transition Next(GameState state, NextRound next, GameModes modes, GameContext context)
    {
        if (state.Phase != GamePhase.BetweenRounds)
        {
            return Transition.Rejected(state, RejectionReason.NotBetweenRounds);
        }

        var finished = state.CurrentRound!;
        if (finished.Id != next.AfterRound)
        {
            return Transition.Rejected(state, RejectionReason.RoundMismatch);
        }

        return Start(state, finished.Index + 1, modes, context);
    }

    public static Transition HandlePlayerIntent(GameState state, PlayerRoundInput input, GameModes modes, GameContext context)
    {
        if (!state.Players.Any(p => p.Id == input.PlayerId))
        {
            return Transition.Rejected(state, RejectionReason.PlayerUnknown);
        }

        return HandleIntent(state, input, input.RoundIntent.RoundId, modes, context);
    }

    public static Transition HandleGameMasterIntent(GameState state, GameMasterRoundInput input, GameModes modes, GameContext context) =>
        HandleIntent(state, input, input.RoundIntent.RoundId, modes, context);

    /// <summary>
    /// Hands a timer to the round that scheduled it, as long as that round is in progress. A timer of a round that is over
    /// never reaches a game mode, which would take it for one of its own.
    /// </summary>
    public static Transition HandleTimer(GameState state, TimerElapsed timer, GameModes modes, GameContext context)
    {
        if (state.Phase != GamePhase.Round || timer.RoundId is null || timer.RoundId != state.CurrentRound!.Id)
        {
            return Transition.Rejected(state, RejectionReason.UnexpectedTimer);
        }

        return Handle(state, timer, modes, context);
    }

    private static Transition HandleIntent(GameState state, GameInput input, RoundId roundId, GameModes modes, GameContext context)
    {
        if (state.Phase != GamePhase.Round)
        {
            return Transition.Rejected(state, RejectionReason.NotInRound);
        }

        if (roundId != state.CurrentRound!.Id)
        {
            return Transition.Rejected(state, RejectionReason.RoundMismatch);
        }

        return Handle(state, input, modes, context);
    }

    private static Transition Handle(GameState state, GameInput input, GameModes modes, GameContext context)
    {
        var round = state.CurrentRound!;
        var handled = modes.For(state.Rounds[round.Index]).Handle(round.State, input, state, context);
        return Apply(state, round, handled);
    }

    /// <summary>
    /// Folds what the game mode did into the game: its new round state, its effects with the timers marked, and the end of
    /// the round, after which the game goes between two rounds, or is finished after the last one.
    /// </summary>
    private static Transition Apply(GameState state, PlayedRound round, RoundTransition handled)
    {
        if (handled.Rejection is { } rejection)
        {
            return Transition.Rejected(state, rejection);
        }

        ImmutableArray<Effect> effects =
            [.. handled.Effects.Select(effect => effect is ScheduleTimer timer ? timer with { RoundId = round.Id } : effect)];

        if (!handled.IsFinished && ReferenceEquals(handled.State, round.State))
        {
            // Accepted, but nothing changes: the same instance tells the loop that there is nothing to broadcast.
            return new Transition(state, effects);
        }

        var phase = !handled.IsFinished ? GamePhase.Round
            : round.Index == state.Rounds.Length - 1 ? GamePhase.Finished
            : GamePhase.BetweenRounds;
        return new Transition(state with { Phase = phase, CurrentRound = round with { State = handled.State } }, effects);
    }

    /// <summary>
    /// A round identifier drawn from the random generator of the context, so that a replayed game gets the same ones.
    /// </summary>
    private static RoundId NewRoundId(Random random)
    {
        Span<byte> bytes = stackalloc byte[16];
        random.NextBytes(bytes);
        return new RoundId(new Guid(bytes));
    }
}
