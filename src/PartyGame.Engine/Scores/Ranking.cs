using System.Collections.Immutable;
using PartyGame.Engine.State;
using PartyGame.Engine.Text;

namespace PartyGame.Engine.Scores;

/// <summary>
/// Ranks the players of a game by their score. The engine alone ranks them: the clients show the ranks they receive.
/// </summary>
public static class Ranking
{
    /// <summary>
    /// Every player by rank, then in alphabetical order of nickname within a rank. Players with the same score share
    /// their rank, and the next rank counts them (1, 1, 3), whether they are connected or joined during the game.
    /// </summary>
    /// <param name="players">The registered players, in any order.</param>
    public static ImmutableArray<Standing> Of(ImmutableArray<Player> players)
    {
        // Alphabetical as players read it, accents and case aside, then by code point so that the order never depends on
        // the culture of the host nor on the order of arrival.
        var ordered = players
            .OrderByDescending(p => p.Score)
            .ThenBy(p => TextComparison.Key(p.Nickname), StringComparer.Ordinal)
            .ThenBy(p => p.Nickname, StringComparer.Ordinal)
            .ToImmutableArray();

        var builder = ImmutableArray.CreateBuilder<Standing>(ordered.Length);
        var rank = 0;
        for (var i = 0; i < ordered.Length; i++)
        {
            var score = ordered[i].Score;
            var tiedWithPrevious = i > 0 && ordered[i - 1].Score == score;
            rank = tiedWithPrevious ? rank : i + 1;
            var tied = tiedWithPrevious || (i < ordered.Length - 1 && ordered[i + 1].Score == score);
            builder.Add(new Standing(ordered[i], rank, tied));
        }

        return builder.MoveToImmutable();
    }
}
