namespace PartyGame.Engine;

/// <summary>
/// What the engine needs from the outside world to handle an input: it never reads a clock nor creates a random generator itself.
/// </summary>
/// <param name="Now">Current server time, from the injected <see cref="TimeProvider"/>.</param>
/// <param name="Random">Random generator created by the caller with a controlled seed, so that tests are reproducible.</param>
public sealed record GameContext(DateTimeOffset Now, Random Random);
