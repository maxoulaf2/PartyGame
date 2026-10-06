using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;

namespace PartyGame.Engine.Rounds;

/// <summary>
/// Pause of the game by the game master, once started and until finished: everything stands still, the phase and the round
/// included, so that the game resumes exactly where it stood. The request names the outcome, so that a double tap or two
/// consoles never pause or resume twice.
/// </summary>
internal static class GamePause
{
    public static Transition Handle(GameState state, PauseGame request, GameModes modes, GameContext context)
    {
        if (state.Phase is not (GamePhase.RoundIntro or GamePhase.Round or GamePhase.BetweenRounds))
        {
            return Transition.Rejected(state, RejectionReason.NotPausable);
        }

        if (request.GameId != state.GameId)
        {
            return Transition.Rejected(state, RejectionReason.GameMismatch);
        }

        if (request.Paused == state.PausedAt is not null)
        {
            return Transition.Rejected(state, RejectionReason.PauseUnchanged);
        }

        if (request.Paused)
        {
            // The deadlines stay as they are: the resumption moves them on by the length of the pause.
            return new Transition(
                state with { PausedAt = context.Now },
                state.Phase == GamePhase.Round ? [new CancelRoundTimers(state.CurrentRound!.Id)] : []);
        }

        var resumed = state with { PausedAt = null };
        if (state.Phase != GamePhase.Round)
        {
            return new Transition(resumed, []);
        }

        // The same way as a game resumed after a restart: the pause costs nobody anything.
        return RoundFlow.ResumeRound(resumed, context.Now - state.PausedAt!.Value, modes, context);
    }
}
