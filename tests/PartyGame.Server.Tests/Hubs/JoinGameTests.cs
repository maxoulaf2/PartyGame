using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

public sealed class JoinGameTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public JoinGameTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("LogFiles:Directory", _logs.Path)
            .UseSetting("GameMaster:Code", Code));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory.Services.GetRequiredService<GameLoop>();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
    }

    [Fact]
    public async Task JoinGame_FreeNickname_ReturnsIdentityAndSendsPlayerSnapshot()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        using var received = new ReceivedSnapshots(connection);

        // When
        var result = await JoinAsync(connection, "  Zoé  ");

        // Then
        Assert.Null(result.Refusal);
        Assert.NotNull(result.PlayerId);
        Assert.Matches("^[A-Za-z0-9_-]{22}$", result.Token);
        await FlushAsync(connection);
        var snapshot = Assert.Single(received.Player.DistinctBy(s => s.Version));
        Assert.Equal(new PlayerSnapshot(Game.State.GameId, 2, Phase.Lobby, result.PlayerId.Value, "Zoé", PlayerCount: 1, Round: null, RoundView: null), snapshot);
        var player = Assert.Single(Game.State.Players);
        Assert.True(player.IsConnected);
        Assert.Equal(result.PlayerId, Game.State.PlayerTokens[new Engine.PlayerToken(result.Token!)]);
        Assert.Equal([HubGroups.Player(result.PlayerId.Value)], await GroupsOfAsync(connection, result.PlayerId.Value));
    }

    [Fact]
    public async Task JoinGame_SeveralPhones_EachGetADistinctIdentity()
    {
        // Given
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);

        // When
        var toZoe = await JoinAsync(zoe, "Zoé");
        var toMax = await JoinAsync(max, "Max");

        // Then
        Assert.NotEqual(toZoe.PlayerId, toMax.PlayerId);
        Assert.NotEqual(toZoe.Token, toMax.Token);
        Assert.Equal(["Zoé", "Max"], Game.State.Players.Select(p => p.Nickname));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Seventeen chars!!")]
    [InlineData("Zo\u200Bé")]
    [InlineData("Zo\u0007é")]
    public async Task JoinGame_InvalidNickname_IsRefusedWithoutJoiningAGroup(string nickname)
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        using var received = new ReceivedSnapshots(connection);

        // When
        var result = await JoinAsync(connection, nickname);

        // Then
        Assert.Equal(new JoinResult(JoinRefusal.NicknameInvalid, PlayerId: null, Token: null), result);
        Assert.Empty(Game.State.Players);
        await FlushAsync(connection);
        Assert.Empty(received.Json);
    }

    [Theory]
    [InlineData("Zoé")]
    [InlineData("zoe")]
    [InlineData(" ZOE ")]
    public async Task JoinGame_TakenNickname_IsRefusedAndLeavesThePlayerGroup(string nickname)
    {
        // Given
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var other = await HubClients.ConnectAsync(_factory);
        using var toOther = new ReceivedSnapshots(other);
        await JoinAsync(zoe, "Zoé");

        // When
        var result = await JoinAsync(other, nickname);
        await JoinAsync(zoe, "Max", expectRefusal: true); // a later change, which a refused phone must not receive

        // Then
        Assert.Equal(new JoinResult(JoinRefusal.NicknameTaken, PlayerId: null, Token: null), result);
        Assert.Single(Game.State.Players);
        await FlushAsync(other);
        Assert.Empty(toOther.Json);
    }

    [Fact]
    public async Task JoinGame_SameNicknameFromManyPhonesAtOnce_RegistersOnlyOne()
    {
        // Given
        var connections = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => HubClients.ConnectAsync(_factory)));
        try
        {
            // When
            var results = await Task.WhenAll(connections.Select(c => JoinAsync(c, "Zoé")));

            // Then
            Assert.Single(results, r => r.Refusal is null);
            Assert.Equal(7, results.Count(r => r.Refusal == JoinRefusal.NicknameTaken));
            Assert.Single(Game.State.Players);
        }
        finally
        {
            foreach (var connection in connections)
            {
                await connection.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task JoinGame_AgainOnTheSameConnection_IsRefusedAsAlreadyJoined()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        await JoinAsync(connection, "Zoé");

        // When
        var result = await JoinAsync(connection, "Max");

        // Then
        Assert.Equal(JoinRefusal.AlreadyJoined, result.Refusal);
        Assert.Null(result.Token);
        Assert.Single(Game.State.Players);
    }

    public static TheoryData<string, string> MalformedRequests => new()
    {
        { "{}", "$" },
        { """{ "nickname": null }""", "$.nickname" },
        { """{ "nickname": 42 }""", "$.nickname" },
        { "\"Zoé\"", "$" },
        { "null", "$" },
    };

    [Theory]
    [MemberData(nameof(MalformedRequests))]
    public async Task JoinGame_MalformedMessage_IsRefusedAndLogsWarning(string message, string expectedPath)
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        using var json = JsonDocument.Parse(message);

        // When
        var result = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, json.RootElement, Ct);

        // Then
        Assert.Equal(JoinRefusal.MessageInvalid, result.Refusal);
        Assert.Empty(Game.State.Players);
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Malformed", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
        Assert.Contains($"\"JsonPath\":\"{expectedPath}\"", warning.Line, StringComparison.Ordinal);
    }

    [Fact]
    public async Task JoinGame_Token_AppearsInNoSnapshotAndNoLog()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        using var toMax = new ReceivedSnapshots(max);
        await display.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(Role.Display, null), Ct);
        await gameMaster.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(Role.GameMaster, Code), Ct);

        // When
        var tokens = new[] { (await JoinAsync(zoe, "Zoé")).Token!, (await JoinAsync(max, "Max")).Token! };

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe), FlushAsync(max));
        Assert.True(toDisplay.Json.Count + toGameMaster.Json.Count + toZoe.Json.Count + toMax.Json.Count >= 3 + 3 + 2 + 1);
        var secrets = tokens.Select(token => new Secret(token, Audience.Everyone)).ToArray();
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, secrets);
        LeakAssert.NoSecretReceived(Viewer.GameMaster, toGameMaster.Json, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Json, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Max"), toMax.Json, secrets);
        var logs = _logs.ReadAllLogs();
        Assert.Contains("joined as", logs, StringComparison.Ordinal);
        Assert.All(tokens, token => Assert.DoesNotContain(token, logs, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Disconnect_LastConnectionOfAPlayer_ShowsThePlayerDisconnected()
    {
        // Given
        var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await JoinAsync(max, "Max");
        var version = Game.State.Version;

        // When
        await zoe.DisposeAsync();

        // Then
        await WaitUntilAsync(() => Game.State.Version > version);
        Assert.Equal([("Zoé", false), ("Max", true)], Game.State.Players.Select(p => (p.Nickname, p.IsConnected)));
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("lost their last connection", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Disconnect_ConnectionWithoutPlayer_ChangesNothing()
    {
        // Given
        await using var zoe = await HubClients.ConnectAsync(_factory);
        var refused = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await JoinAsync(refused, "zoe", expectRefusal: true);
        var version = Game.State.Version;

        // When
        await refused.DisposeAsync();
        await JoinAsync(zoe, "Max", expectRefusal: true); // goes through the queue after any input of the disconnection

        // Then
        Assert.Equal(version, Game.State.Version);
        Assert.True(Assert.Single(Game.State.Players).IsConnected);
    }

    private static async Task<JoinResult> JoinAsync(HubConnection connection, string nickname, bool expectRefusal = false)
    {
        var result = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        if (expectRefusal)
        {
            Assert.NotNull(result.Refusal);
        }

        return result;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    private Task<IReadOnlyList<string>> GroupsOfAsync(HubConnection connection, PlayerId playerId) =>
        HubClients.GroupsOfAsync<GameHub>(_factory, connection, HubGroups.Display, HubGroups.GameMaster, HubGroups.Player(playerId));

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
