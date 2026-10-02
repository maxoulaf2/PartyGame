using PartyGame.Contracts;
using PartyGame.Engine.Inputs;
using PartyGame.Server.Hubs;
using PartyGame.Server.Tests.Games;

namespace PartyGame.Server.Tests.Hubs;

public sealed class PlayerConnectionsTests
{
    private static readonly PlayerId _zoe = new(new Guid(1, 0, 0, new byte[8]));
    private static readonly PlayerId _max = new(new Guid(2, 0, 0, new byte[8]));

    private readonly RecordingInputWriter _inputs = new();
    private readonly PlayerConnections _connections;

    public PlayerConnectionsTests()
    {
        _connections = new PlayerConnections(_inputs);
    }

    [Fact]
    public async Task DisconnectAsync_OnlyConnectionOfARegisteredPlayer_ReportsTheLoss()
    {
        // Given
        _connections.Register(_zoe, "a");

        // When
        var wasLast = await _connections.DisconnectAsync(_zoe, "a");

        // Then
        Assert.True(wasLast);
        Assert.Equal([new PlayerConnectionLost(_zoe)], _inputs.Inputs);
    }

    [Fact]
    public async Task ResumeAsync_WithoutOtherConnection_ReportsThePlayerConnectedAgain()
    {
        // Given
        _connections.Register(_zoe, "a");
        await _connections.DisconnectAsync(_zoe, "a");

        // When
        await _connections.ResumeAsync(_zoe, "b");

        // Then
        Assert.Equal([new PlayerConnectionLost(_zoe), new PlayerConnectionRestored(_zoe)], _inputs.Inputs);
    }

    [Fact]
    public async Task ResumeAsync_WhileAnotherConnectionIsOpen_ReportsNothingUntilTheLastCloses()
    {
        // Given
        _connections.Register(_zoe, "a");

        // When
        await _connections.ResumeAsync(_zoe, "b");
        var firstWasLast = await _connections.DisconnectAsync(_zoe, "a");
        var secondWasLast = await _connections.DisconnectAsync(_zoe, "b");

        // Then
        Assert.False(firstWasLast);
        Assert.True(secondWasLast);
        Assert.Equal([new PlayerConnectionLost(_zoe)], _inputs.Inputs);
    }

    [Fact]
    public async Task DisconnectAsync_UnknownOrAlreadyClosedConnection_ReportsNothing()
    {
        // Given
        _connections.Register(_zoe, "a");
        await _connections.DisconnectAsync(_zoe, "a");

        // When
        var again = await _connections.DisconnectAsync(_zoe, "a");
        var other = await _connections.DisconnectAsync(_max, "a");

        // Then
        Assert.False(again);
        Assert.False(other);
        Assert.Equal([new PlayerConnectionLost(_zoe)], _inputs.Inputs);
    }

    [Fact]
    public async Task ResumeAndDisconnect_ManyAtOnce_AlternateForEachPlayer()
    {
        // Given
        _connections.Register(_zoe, "initial");
        _connections.Register(_max, "initial");
        await _connections.DisconnectAsync(_zoe, "initial");

        // When
        await Parallel.ForAsync(0, 200, async (i, _) =>
        {
            var player = i % 2 == 0 ? _zoe : _max;
            var connection = $"tab-{i}";
            await _connections.ResumeAsync(player, connection);
            await _connections.DisconnectAsync(player, connection);
        });

        // Then: whatever the interleaving, the inputs of a player alternate as the engine expects, none rejected.
        foreach (var player in new[] { _zoe, _max })
        {
            var connected = true; // both registered, so connected before their first input
            foreach (var input in _inputs.Inputs)
            {
                if (input is PlayerConnectionLost lost && lost.PlayerId == player)
                {
                    Assert.True(connected);
                    connected = false;
                }
                else if (input is PlayerConnectionRestored restored && restored.PlayerId == player)
                {
                    Assert.False(connected);
                    connected = true;
                }
            }
        }

        Assert.DoesNotContain(new PlayerConnectionLost(_max), _inputs.Inputs); // its first connection is still open
    }
}
