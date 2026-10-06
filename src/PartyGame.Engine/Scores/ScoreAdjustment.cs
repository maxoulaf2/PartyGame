using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Scores;

/// <summary>
/// Correction of a score by the game master, once the game is started, even paused or finished. The request names the
/// score it corrects, so that a request sent twice or two consoles never adjust it twice.
/// </summary>
internal static class ScoreAdjustment
{
    public static Transition Adjust(GameState state, AdjustScore request)
    {
        if (state.Phase is not (GamePhase.RoundIntro or GamePhase.Round or GamePhase.BetweenRounds or GamePhase.Finished))
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
            // Accepted, but nothing changes: the same instance tells the loop that there is nothing to broadcast.
            return new Transition(state, []);
        }

        return new Transition(state with { Players = state.Players.Replace(player, player with { Score = request.NewScore }) }, []);
    }
}
