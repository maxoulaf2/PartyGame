namespace PartyGame.Contracts;

/// <summary>
/// How well a player or the TV screen reaches the server, for the game master console.
/// </summary>
/// <param name="PlayerId">The player, or <see langword="null"/> for the TV screen.</param>
/// <param name="Transport">How their last connection reaches the hub.</param>
/// <param name="RoundTrip">
/// The round trip of their last clock synchronization, in milliseconds, or <see langword="null"/> before the first.
/// </param>
/// <param name="Reconnections">How many times they connected again since the server started.</param>
public sealed record ConnectionQuality(PlayerId? PlayerId, ConnectionTransport Transport, int? RoundTrip, int Reconnections);
