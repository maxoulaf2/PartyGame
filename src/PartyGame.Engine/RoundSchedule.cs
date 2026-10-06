using System.Collections.Immutable;

namespace PartyGame.Engine;

/// <summary>
/// The programme of a game: every round of its pack but the current one, by position in the pack. Together with the
/// current round, its lists hold each round of the pack exactly once.
/// </summary>
/// <param name="Past">The rounds played or skipped before the current one, in the order they were.</param>
/// <param name="Skipped">Those of <paramref name="Past"/> the game master skipped.</param>
/// <param name="Upcoming">The rounds to play after the current one, in this order.</param>
/// <param name="Withdrawn">The rounds the game master withdrew: they are not played unless put back.</param>
public sealed record RoundSchedule(
    ImmutableArray<int> Past,
    ImmutableArray<int> Skipped,
    ImmutableArray<int> Upcoming,
    ImmutableArray<int> Withdrawn)
{
    /// <summary>The programme of a game not started yet.</summary>
    public static RoundSchedule Empty { get; } = new([], [], [], []);

    /// <summary>
    /// The programme that follows the order of the pack, the round at <paramref name="current"/> being the current one, as
    /// for a game saved before the programme existed.
    /// </summary>
    /// <param name="current">Position of the current round in the pack.</param>
    /// <param name="roundCount">Number of rounds of the pack.</param>
    public static RoundSchedule InPackOrder(int current, int roundCount) =>
        new([.. Enumerable.Range(0, current)], [], [.. Enumerable.Range(current + 1, roundCount - current - 1)], []);

    /// <summary>Number of rounds of the game: those past, the current one and those to come, never those withdrawn.</summary>
    internal int Count => Past.Length + 1 + Upcoming.Length;
}
