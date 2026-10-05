using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Server.Network;

namespace PartyGame.Server.Tests.Network;

public sealed class NetworkHealthJournalTests
{
    private static readonly DateTimeOffset _start = new(2026, 10, 4, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(_start);
    private readonly NetworkHealthJournal _journal;

    public NetworkHealthJournalTests() => _journal = new NetworkHealthJournal(_time);

    [Fact]
    public void RecordDiagnostic_SeveralDiagnostics_ListsTheMostRecentFirstWithTheTimeOfTheServer()
    {
        _journal.RecordDiagnostic(DeviceKind.IPhone, DiagnosticVerdict.Good, 12);
        _time.Advance(TimeSpan.FromSeconds(30));

        _journal.RecordDiagnostic(DeviceKind.Android, DiagnosticVerdict.Problem, roundTripMedian: null);

        Assert.Equal(
            [
                new NetworkDiagnostic(DeviceKind.Android, _start.AddSeconds(30).ToUnixTimeMilliseconds(), DiagnosticVerdict.Problem, null),
                new NetworkDiagnostic(DeviceKind.IPhone, _start.ToUnixTimeMilliseconds(), DiagnosticVerdict.Good, 12),
            ],
            _journal.Current.Diagnostics);
    }

    [Fact]
    public void RecordDiagnostic_BeyondCapacity_ForgetsTheOldest()
    {
        for (var i = 0; i <= NetworkHealthJournal.DiagnosticCapacity; i++)
        {
            _journal.RecordDiagnostic(DeviceKind.Android, DiagnosticVerdict.Good, roundTripMedian: i);
        }

        var diagnostics = _journal.Current.Diagnostics;
        Assert.Equal(NetworkHealthJournal.DiagnosticCapacity, diagnostics.Length);
        Assert.Equal((NetworkHealthJournal.DiagnosticCapacity, 1), (diagnostics[0].RoundTripMedian, diagnostics[^1].RoundTripMedian));
    }

    [Fact]
    public void PlayerConnected_AgainOnAnotherTransport_CountsAReconnectionAndKeepsTheLastRoundTrip()
    {
        var zoe = new PlayerId(Guid.NewGuid());
        _journal.PlayerConnected(zoe, ConnectionTransport.WebSockets, roundTrip: 18);

        _journal.PlayerConnected(zoe, ConnectionTransport.LongPolling, roundTrip: null);

        Assert.Equal([new ConnectionQuality(zoe, ConnectionTransport.LongPolling, 18, Reconnections: 1)], _journal.Current.Connections);
    }

    [Fact]
    public void RecordRoundTrip_PlayerAndDisplay_UpdatesTheirsOnly()
    {
        var zoe = new PlayerId(Guid.NewGuid());
        _journal.DisplayConnected(ConnectionTransport.WebSockets, roundTrip: null);
        _journal.PlayerConnected(zoe, ConnectionTransport.WebSockets, roundTrip: null);

        _journal.RecordRoundTrip(zoe, 25);
        _journal.RecordRoundTrip(playerId: null, 4);
        _journal.RecordRoundTrip(new PlayerId(Guid.NewGuid()), 99);

        Assert.Equal(
            [
                new ConnectionQuality(PlayerId: null, ConnectionTransport.WebSockets, 4, Reconnections: 0),
                new ConnectionQuality(zoe, ConnectionTransport.WebSockets, 25, Reconnections: 0),
            ],
            _journal.Current.Connections);
    }

    [Fact]
    public void DisplayConnected_Again_CountsAReconnection()
    {
        _journal.DisplayConnected(ConnectionTransport.WebSockets, roundTrip: 3);

        _journal.DisplayConnected(ConnectionTransport.WebSockets, roundTrip: null);

        Assert.Equal([new ConnectionQuality(PlayerId: null, ConnectionTransport.WebSockets, 3, Reconnections: 1)], _journal.Current.Connections);
    }

    [Fact]
    public void RecordDisplayAudio_ThenReconnection_KeepsItForTheDisplayOnly()
    {
        var zoe = new PlayerId(Guid.NewGuid());
        _journal.DisplayConnected(ConnectionTransport.WebSockets, roundTrip: 3);
        _journal.PlayerConnected(zoe, ConnectionTransport.WebSockets, roundTrip: 25);

        _journal.RecordDisplayAudio(unlocked: true);
        _journal.DisplayConnected(ConnectionTransport.WebSockets, roundTrip: null);

        Assert.Equal(
            [
                new ConnectionQuality(PlayerId: null, ConnectionTransport.WebSockets, 3, Reconnections: 1, AudioUnlocked: true),
                new ConnectionQuality(zoe, ConnectionTransport.WebSockets, 25, Reconnections: 0),
            ],
            _journal.Current.Connections);
    }
}
