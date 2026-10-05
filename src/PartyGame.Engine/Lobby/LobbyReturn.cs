using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// End of the game by the game master, at any moment once started: back to the lobby for a new game, without restarting the
/// server. The players stay registered, so that their phones need no action, but their scores start over.
/// </summary>
internal static class LobbyReturn
{
    public static Transition Return(GameState state, ReturnToLobby request, GameContext context)
    {
        if (state.Phase == GamePhase.Lobby)
        {
            return Transition.Rejected(state, RejectionReason.GameNotStarted);
        }

        if (request.GameId != state.GameId)
        {
            return Transition.Rejected(state, RejectionReason.GameMismatch);
        }

        // A round still in progress may have timers that would otherwise elapse in the new game.
        ImmutableArray<Effect> effects = state is { Phase: GamePhase.Round, CurrentRound: { } round }
            ? [new CancelRoundTimers(round.Id)]
            : [];

        // A new identifier makes the request obsolete once handled: sent again, it never ends the new game. The catalog,
        // the pack chosen and the address are kept, so that the same pack can be played again at once.
        return new Transition(
            state with
            {
                GameId = NewGameId(context.Random),
                Phase = GamePhase.Lobby,
                Players = [.. state.Players.Select(p => p with { Score = 0, JoinedAfterEnd = false })],
                Pack = null,
                Media = PackMedia.Empty,
                CurrentRound = null,
            },
            effects);
    }

    /// <summary>
    /// A game identifier drawn from the random generator of the context, so that a replayed game gets the same ones.
    /// </summary>
    private static GameId NewGameId(Random random)
    {
        Span<byte> bytes = stackalloc byte[16];
        random.NextBytes(bytes);
        return new GameId(new Guid(bytes));
    }
}
