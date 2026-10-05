namespace PartyGame.Engine;

/// <summary>
/// What the engine needs from the outside world to handle an input: it never reads a clock nor creates a random generator itself.
/// </summary>
/// <param name="Now">Current server time, from the injected <see cref="TimeProvider"/>.</param>
/// <param name="Random">Random generator created by the caller with a controlled seed, so that tests are reproducible.</param>
public sealed record GameContext(DateTimeOffset Now, Random Random)
{
    /// <summary>
    /// The default of <see cref="BuzzerArbitrationWindow"/>.
    /// </summary>
    public static readonly TimeSpan DefaultBuzzerArbitrationWindow = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// How long a buzzer waits after the first buzz of an opening before it designates the winner, so that a player who
    /// pressed earlier but whose buzz arrives later still wins. Set by the server from its configuration.
    /// </summary>
    public TimeSpan BuzzerArbitrationWindow { get; init; } = DefaultBuzzerArbitrationWindow;
}
