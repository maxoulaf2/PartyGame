namespace PartyGame.Bots;

/// <summary>
/// Reads the <c>--behavior</c> option: one behavior for every bot, or a mix that tells how many bots follow each one.
/// </summary>
internal static class BotBehaviors
{
    /// <summary>
    /// The behavior of each bot, in order, or null when the option is invalid or its mix does not add up to
    /// <paramref name="count"/>.
    /// </summary>
    /// <param name="spec"><c>random</c>, or a mix such as <c>random:6,flaky:2,silent:2</c>.</param>
    /// <param name="count">The number of bots, or null for that of the mix, or a single bot.</param>
    public static IReadOnlyList<BotBehavior>? Parse(string spec, int? count)
    {
        if (!spec.Contains(':', StringComparison.Ordinal))
        {
            return TryParse(spec, out var behavior) ? Enumerable.Repeat(behavior, count ?? 1).ToList() : null;
        }

        var behaviors = new List<BotBehavior>();
        foreach (var part in spec.Split(','))
        {
            if (part.Split(':') is not [var name, var number]
                || !TryParse(name, out var behavior)
                || !int.TryParse(number, out var times)
                || times < 1)
            {
                return null;
            }

            behaviors.AddRange(Enumerable.Repeat(behavior, times));
        }

        return count is null || count == behaviors.Count ? behaviors : null;
    }

    private static bool TryParse(string name, out BotBehavior behavior) =>
        Enum.TryParse(name, ignoreCase: true, out behavior) && Enum.IsDefined(behavior);
}
