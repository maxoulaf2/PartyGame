using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Rounds;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Start of the game by the game master, once: the first round of the pack starts. Registration stays open afterwards.
/// </summary>
internal static class Launch
{
    /// <summary>
    /// Registered players, connected or not, below which the game cannot start. One player eases the tests; a higher
    /// value may become configurable.
    /// </summary>
    public const int MinimumPlayerCount = 1;

    public static Transition Start(GameState state, StartGame start, GameModes modes, GameContext context)
    {
        if (state.Phase != GamePhase.Lobby)
        {
            return Transition.Rejected(state, RejectionReason.GameAlreadyStarted);
        }

        if (state.Players.Length < MinimumPlayerCount)
        {
            return Transition.Rejected(state, RejectionReason.NotEnoughPlayers);
        }

        // Checked once and for all here, so that no round can fail to start in the middle of the game.
        if (state.Rounds.Any(descriptor => !modes.TryFind(descriptor, out _)))
        {
            return Transition.Rejected(state, RejectionReason.GameModeMissing);
        }

        if (state.Rounds.IsEmpty)
        {
            // Until the game master chooses a pack (US-E06-03), the game has no round to play.
            return new Transition(state with { Phase = GamePhase.Finished }, []);
        }

        return RoundFlow.Start(state, index: 0, modes, context);
    }
}
