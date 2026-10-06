using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Scores;

namespace PartyGame.Engine.Rounds;

/// <summary>
/// Sequence of the rounds of the pack: each one is announced, started by the game master, fed with the inputs aimed at it
/// and ended by its game mode, then the game master asks for the next one, until the last one.
/// </summary>
internal static class RoundFlow
{
    /// <summary>
    /// Announces a round of the pack, which its game mode does not play until the game master starts it. The ranking the
    /// players leave is remembered, for the next one to show who gained or lost places: none before the first round.
    /// </summary>
    public static Transition Announce(GameState state, int index, GameContext context)
    {
        var ranks = index == 0 ? [] : Ranking.Of(state.Players).ToDictionary(s => s.Player.Id, s => s.Rank);
        return new(
            state with
            {
                Phase = GamePhase.RoundIntro,
                Players = [.. state.Players.Select(p => p with { PreviousRank = ranks.TryGetValue(p.Id, out var rank) ? rank : null })],
                CurrentRound = new PlayedRound(NewRoundId(context.Random), index, State: null),
            },
            []);
    }

    /// <summary>
    /// Starts the round announced that the game master names: a request sent twice, or by two consoles at once, starts it
    /// only once. Every activity has a game mode: the start of the game checked it.
    /// </summary>
    public static Transition Start(GameState state, StartRound start, GameModes modes, GameContext context)
    {
        if (state.Phase != GamePhase.RoundIntro)
        {
            return Transition.Rejected(state, RejectionReason.NoRoundAnnounced);
        }

        var round = state.CurrentRound!;
        if (round.Id != start.RoundId)
        {
            return Transition.Rejected(state, RejectionReason.RoundMismatch);
        }

        var descriptor = state.Rounds[round.Index];
        var started = modes.For(descriptor).Start(descriptor, state, context);
        if (started.Rejection is { } rejection)
        {
            throw new InvalidOperationException($"Game mode for {descriptor.GetType().Name} rejected the start of a round: {rejection}.");
        }

        var playing = round with { State = started.State };
        return Apply(state with { Phase = GamePhase.Round, CurrentRound = playing }, playing, started, context);
    }

    /// <summary>
    /// Announces the round that follows the one the game master names, which must be the one that just finished: a request
    /// sent twice, or by two consoles at once, announces it only once.
    /// </summary>
    public static Transition Next(GameState state, NextRound next, GameContext context)
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

        return Announce(state, finished.Index + 1, context);
    }

    /// <summary>
    /// Ends the round in progress, or announced, that the game master names, without its game mode, since it may be what
    /// keeps failing, even to start.
    /// The points already added to the scores stay, those its question in progress would award are never awarded, and its
    /// timers are cancelled. The game then goes between two rounds, or is finished after the last one, as when the mode
    /// ends the round itself. A request sent twice, or by two consoles at once, skips the round only once.
    /// </summary>
    public static Transition Skip(GameState state, SkipRound skip, GameContext context)
    {
        if (state.Phase is not (GamePhase.Round or GamePhase.RoundIntro))
        {
            return Transition.Rejected(state, RejectionReason.NotInRound);
        }

        var round = state.CurrentRound!;
        if (round.Id != skip.RoundId)
        {
            return Transition.Rejected(state, RejectionReason.RoundMismatch);
        }

        return new Transition(
            Ended(state, context) with { CurrentRound = round with { IsSkipped = true } },
            [new CancelRoundTimers(round.Id)]);
    }

    /// <summary>
    /// Hands an intent of a player to the round it names, once: an intent numbered up to the last one accepted from the
    /// player was already handled, and never reaches the game mode again. Only an accepted intent counts as handled: a
    /// rejected one leaves the state as it is, and is judged again if sent again.
    /// </summary>
    public static Transition HandlePlayerIntent(GameState state, PlayerRoundInput input, GameModes modes, GameContext context)
    {
        var player = state.Players.FirstOrDefault(p => p.Id == input.PlayerId);
        if (player is null)
        {
            return Transition.Rejected(state, RejectionReason.PlayerUnknown);
        }

        if (input.ClientSeq <= player.LastClientSeq)
        {
            return Transition.Rejected(state, RejectionReason.IntentAlreadyHandled);
        }

        var handled = HandleIntent(state, input, input.RoundIntent.RoundId, modes, context);
        if (handled.Rejection is not null)
        {
            return handled;
        }

        // Even when the mode changes nothing, so that the intent sent again is not handled again. The player as the mode
        // left them, with the points it may have awarded for the intent.
        var acting = handled.State.Players.First(p => p.Id == player.Id);
        var players = handled.State.Players.Replace(acting, acting with { LastClientSeq = input.ClientSeq });
        return new Transition(handled.State with { Players = players }, handled.Effects);
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

    /// <summary>
    /// Hands the time spent offline to the round in progress of a game just resumed, for it to move its deadlines on and
    /// schedule its timers again. Out of a round, nothing waits for any time: there is nothing to resume.
    /// </summary>
    public static Transition Resume(GameState state, GameResumed resumed, GameModes modes, GameContext context)
    {
        if (state.Phase != GamePhase.Round)
        {
            return new Transition(state, []);
        }

        var round = state.CurrentRound!;
        var descriptor = state.Rounds[round.Index];
        var handled = modes.For(descriptor).ResumeRound(round.State!, state, context.Now - resumed.SavedAt, context);
        if (handled.Rejection is { } rejection)
        {
            throw new InvalidOperationException($"Game mode for {descriptor.GetType().Name} rejected the resumption of a round: {rejection}.");
        }

        return Apply(state, round, handled, context);
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
        var handled = modes.For(state.Rounds[round.Index]).Handle(round.State!, input, state, context);
        return Apply(state, round, handled, context);
    }

    /// <summary>
    /// Folds what the game mode did into the game: its new round state, its effects with the timers marked, the points it
    /// awards, and the end of the round, after which the game goes between two rounds, or is finished after the last one.
    /// </summary>
    private static Transition Apply(GameState state, PlayedRound round, RoundTransition handled, GameContext context)
    {
        if (handled.Rejection is { } rejection)
        {
            return Transition.Rejected(state, rejection);
        }

        ImmutableArray<Effect> effects =
            [.. handled.Effects.Select(effect => effect is ScheduleTimer timer ? timer with { RoundId = round.Id } : effect)];

        if (!handled.IsFinished && ReferenceEquals(handled.State, round.State) && handled.Points.IsEmpty)
        {
            // Accepted, but nothing changes: the same instance tells the loop that there is nothing to broadcast.
            return new Transition(state, effects);
        }

        var played = state with
        {
            Players = Award(state.Players, handled.Points),
            CurrentRound = round with { State = handled.State },
        };
        return new Transition(handled.IsFinished ? Ended(played, context) : played, effects);
    }

    /// <summary>
    /// The game once its current round is over: between two rounds, or finished after the last one, from now on.
    /// </summary>
    private static GameState Ended(GameState state, GameContext context) =>
        state.CurrentRound!.Index == state.Rounds.Length - 1
            ? state with { Phase = GamePhase.Finished, FinishedAt = context.Now }
            : state with { Phase = GamePhase.BetweenRounds };

    /// <summary>
    /// Adds the points a round awards to the scores of the players. Players are never removed, so every player a round
    /// awards points to is still registered.
    /// </summary>
    private static ImmutableArray<Player> Award(ImmutableArray<Player> players, ImmutableDictionary<PlayerId, int> points) =>
        points.IsEmpty
            ? players
            : [.. players.Select(player => points.TryGetValue(player.Id, out var earned) ? player with { Score = player.Score + earned } : player)];

    /// <summary>
    /// A round identifier drawn from the random generator of the context, so that a replayed game gets the same ones.
    /// </summary>
    internal static RoundId NewRoundId(Random random)
    {
        Span<byte> bytes = stackalloc byte[16];
        random.NextBytes(bytes);
        return new RoundId(new Guid(bytes));
    }
}
