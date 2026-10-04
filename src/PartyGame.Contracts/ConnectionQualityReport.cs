namespace PartyGame.Contracts;

/// <summary>
/// A page tells the server the round trip it measured with its last clock synchronization, so that the game master sees
/// how well each phone and the TV screen reach the server. Only those of the players and of the TV screen are kept.
/// </summary>
/// <param name="RoundTrip">The shortest round trip of the burst, in milliseconds.</param>
public sealed record ConnectionQualityReport(int RoundTrip);
