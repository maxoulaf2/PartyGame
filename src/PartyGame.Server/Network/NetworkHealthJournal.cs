using PartyGame.Contracts;

namespace PartyGame.Server.Network;

/// <summary>
/// How the devices reach the server, for the game master: the last network diagnostics, and the quality of the connection
/// of each player and of the TV screen. Kept apart from the game state, which these measures would change every few
/// seconds, and forgotten on restart.
/// </summary>
/// <remarks>Fed by the hub from several connections at once: the lock guards it.</remarks>
internal sealed class NetworkHealthJournal(TimeProvider timeProvider)
{
    /// <summary>The most diagnostics kept: those of an arrival on site, before the guests come.</summary>
    public const int DiagnosticCapacity = 20;

    private readonly Lock _gate = new();

    // The most recent first, as the console lists them.
    private readonly List<NetworkDiagnostic> _diagnostics = [];
    private readonly Dictionary<PlayerId, ConnectionQuality> _players = [];

    // ponytail: one entry for every TV screen, two screens would count each other's connections as reconnections.
    private ConnectionQuality? _display;

    public NetworkHealth Current
    {
        get
        {
            lock (_gate)
            {
                return new NetworkHealth([.. _diagnostics], [.. _display is null ? [] : new[] { _display }, .. _players.Values]);
            }
        }
    }

    public void RecordDiagnostic(DeviceKind device, DiagnosticVerdict verdict, int? roundTripMedian)
    {
        var now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        lock (_gate)
        {
            _diagnostics.Insert(0, new NetworkDiagnostic(device, now, verdict, roundTripMedian));
            if (_diagnostics.Count > DiagnosticCapacity)
            {
                _diagnostics.RemoveAt(_diagnostics.Count - 1);
            }
        }
    }

    /// <summary>
    /// Records a connection of a player: their first one when they register, another one when they resume their session.
    /// </summary>
    /// <param name="playerId">The player.</param>
    /// <param name="transport">How the connection reaches the hub.</param>
    /// <param name="roundTrip">The round trip the connection reported before it identified, if any.</param>
    public void PlayerConnected(PlayerId playerId, ConnectionTransport transport, int? roundTrip)
    {
        lock (_gate)
        {
            _players[playerId] = _players.TryGetValue(playerId, out var previous)
                ? previous with { Transport = transport, RoundTrip = roundTrip ?? previous.RoundTrip, Reconnections = previous.Reconnections + 1 }
                : new ConnectionQuality(playerId, transport, roundTrip, Reconnections: 0);
        }
    }

    /// <summary>Records a connection announced as the TV screen, another one counting as a reconnection.</summary>
    /// <param name="transport">How the connection reaches the hub.</param>
    /// <param name="roundTrip">The round trip the connection reported before it announced, if any.</param>
    public void DisplayConnected(ConnectionTransport transport, int? roundTrip)
    {
        lock (_gate)
        {
            _display = _display is { } previous
                ? previous with { Transport = transport, RoundTrip = roundTrip ?? previous.RoundTrip, Reconnections = previous.Reconnections + 1 }
                : new ConnectionQuality(PlayerId: null, transport, roundTrip, Reconnections: 0);
        }
    }

    /// <summary>Records the last round trip of a player, or of the TV screen when <paramref name="playerId"/> is null.</summary>
    public void RecordRoundTrip(PlayerId? playerId, int roundTrip)
    {
        lock (_gate)
        {
            if (playerId is null)
            {
                _display = _display is null ? null : _display with { RoundTrip = roundTrip };
            }
            else if (_players.TryGetValue(playerId.Value, out var quality))
            {
                _players[playerId.Value] = quality with { RoundTrip = roundTrip };
            }
        }
    }
}
