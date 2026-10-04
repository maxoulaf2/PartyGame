namespace PartyGame.Bots;

/// <summary>
/// How a simulated player plays.
/// </summary>
internal enum BotBehavior
{
    /// <summary>A choice shown at random, after a random delay.</summary>
    Random,

    /// <summary>The first choice shown, at once.</summary>
    Fast,

    /// <summary>A choice at random, just before the deadline.</summary>
    Slow,

    /// <summary>Never answers.</summary>
    Silent,

    /// <summary>Plays as <see cref="Random"/>, but drops its connection and comes back at random times.</summary>
    Flaky,
}
