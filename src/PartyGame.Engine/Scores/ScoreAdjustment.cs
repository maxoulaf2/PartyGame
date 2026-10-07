using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Scores;

/// <summary>
/// Correction of a score by the game master, once the game is started, even paused or finished. The request names the
/// score it corrects, so that a request sent twice or two consoles never adjust it twice.
/// </summary>
internal static class ScoreAdjustment
{
    public static Transition Adjust(GameState state, AdjustScore request)
    {
        if (!state.IsInProgress && state.Phase != GamePhase.Finished)
        {
            return Transition.Rejected(state, RejectionReason.NotAdjustable);
        }

        var player = state.Players.FirstOrDefault(p => p.Id == request.PlayerId);
        if (player is null)
        {
            return Transition.Rejected(state, RejectionReason.PlayerUnknown);
        }

        if (request.NewScore < 0)
        {
            return Transition.Rejected(state, RejectionReason.ScoreNegative);
        }

        if (request.ExpectedScore != player.Score)
        {
            return Transition.Rejected(state, RejectionReason.ScoreObsolete);
        }

        if (request.NewScore == player.Score)
        {
            return Transition.Unchanged(state);
        }

        return new Transition(state with { Players = state.Players.Replace(player, player with { Score = request.NewScore }) }, []);
    }
}
