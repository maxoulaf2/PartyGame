namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub to a clock synchronization: the time of the server when it answered. The client brackets the
/// call with its own clock to estimate the offset between the two and the round-trip time, as NTP does.
/// </summary>
/// <param name="ServerTime">The time of the server, in milliseconds since the Unix epoch.</param>
public sealed record ClockSyncResult(long ServerTime);
