using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Rounds;

/// <summary>
/// Change of the programme by the game master, once the game is started and until it is finished, even paused: the rounds
/// to come are reordered, withdrawn or put back. The rounds played and the current one never move. The request names the
/// programme it replaces, so that a request sent twice or two consoles never change it twice.
/// </summary>
internal static class Programme
{
    public static Transition Reorder(GameState state, ReorderRounds request)
    {
        if (state.Phase is not (GamePhase.RoundIntro or GamePhase.Round or GamePhase.BetweenRounds))
        {
            return Transition.Rejected(state, RejectionReason.NotReorderable);
        }

        if (request.GameId != state.GameId)
        {
            return Transition.Rejected(state, RejectionReason.GameMismatch);
        }

        var schedule = state.Schedule;
        var order = OrderOf(schedule);
        if (!request.ExpectedOrder.SequenceEqual(order))
        {
            return Transition.Rejected(state, RejectionReason.ScheduleObsolete);
        }

        foreach (var round in request.NewOrder)
        {
            if (round.RoundIndex < 0 || round.RoundIndex >= state.Rounds.Length)
            {
                return Transition.Rejected(state, RejectionReason.RoundUnknown);
            }

            if (round.RoundIndex == state.CurrentRound!.Index || schedule.Past.Contains(round.RoundIndex))
            {
                return Transition.Rejected(state, RejectionReason.RoundFixed);
            }
        }

        var indexes = request.NewOrder.Select(r => r.RoundIndex).ToList();
        if (indexes.Count != order.Length || indexes.Distinct().Count() != indexes.Count)
        {
            return Transition.Rejected(state, RejectionReason.ScheduleIncomplete);
        }

        if (request.NewOrder.SequenceEqual(order))
        {
            // Accepted, but nothing changes: the same instance tells the loop that there is nothing to broadcast.
            return new Transition(state, []);
        }

        return new Transition(
            state with
            {
                Schedule = schedule with
                {
                    Upcoming = [.. request.NewOrder.Where(r => !r.IsWithdrawn).Select(r => r.RoundIndex)],
                    Withdrawn = [.. request.NewOrder.Where(r => r.IsWithdrawn).Select(r => r.RoundIndex)],
                },
            },
            []);
    }

    /// <summary>The rounds to come then those withdrawn, as a request names them.</summary>
    private static ImmutableArray<ScheduledRound> OrderOf(RoundSchedule schedule) =>
        [.. schedule.Upcoming.Select(i => new ScheduledRound(i, IsWithdrawn: false)), .. schedule.Withdrawn.Select(i => new ScheduledRound(i, IsWithdrawn: true))];
}
