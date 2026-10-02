using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Start of the game by the game master, once. Registration stays open afterwards.
/// </summary>
internal static class Launch
{
    /// <summary>
    /// Registered players, connected or not, below which the game cannot start. One player eases the tests; a higher
    /// value may become configurable.
    /// </summary>
    public const int MinimumPlayerCount = 1;

    public static Transition Start(GameState state, StartGame start)
    {
        if (state.Phase != GamePhase.Lobby)
        {
            return Transition.Rejected(state, RejectionReason.GameAlreadyStarted);
        }

        if (state.Players.Length < MinimumPlayerCount)
        {
            return Transition.Rejected(state, RejectionReason.NotEnoughPlayers);
        }

        return new Transition(state with { Phase = GamePhase.Started }, []);
    }
}
