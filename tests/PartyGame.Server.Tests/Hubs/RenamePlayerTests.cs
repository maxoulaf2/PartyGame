using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class RenamePlayerTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public RenamePlayerTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
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
    public async Task RenamePlayer_AsGameMaster_ShowsTheNewNicknameOnEveryInterface()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        await AnnounceAsync(display, Role.Display);
        var zoeId = await JoinAsync(zoe, "Zoé");
        var maxId = await JoinAsync(max, "Max");

        // When
        var result = await RenameAsync(gameMaster, zoeId, "  Zoé   B ");

        // Then
        Assert.Equal(new RenamePlayerResult(Refusal: null), result);
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        Assert.Equal(
            [new GameMasterPlayer(zoeId, "Zoé B", IsConnected: true, Score: 0), new GameMasterPlayer(maxId, "Max", IsConnected: true, Score: 0)],
            Latest(toGameMaster.GameMaster).Players);
        Assert.Equal(["Zoé B", "Max"], Latest(toDisplay.Display).Players.Select(p => p.Nickname));
        Assert.Equal("Zoé B", Latest(toZoe.Player).Nickname);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("renamed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RenamePlayer_DisconnectedPlayer_IsRenamedAndStaysDisconnected()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        var zoe = await HubClients.ConnectAsync(_factory);
        var zoeId = await JoinAsync(zoe, "Zoé");
        var version = Game.State.Version;
        await zoe.DisposeAsync();
        await WaitUntilAsync(() => Game.State.Version > version);

        // When
        var result = await RenameAsync(gameMaster, zoeId, "Léa");

        // Then
        Assert.Null(result?.Refusal);
        Assert.Equal(("Léa", false), Game.State.Players.Select(p => (p.Nickname, p.IsConnected)).Single());
    }

    [Theory]
    [InlineData("Max", RenamePlayerRefusal.NicknameTaken)]
    [InlineData(" MAX ", RenamePlayerRefusal.NicknameTaken)]
    [InlineData("", RenamePlayerRefusal.NicknameInvalid)]
    [InlineData("Seventeen chars!!", RenamePlayerRefusal.NicknameInvalid)]
    [InlineData("Zo​é", RenamePlayerRefusal.NicknameInvalid)]
    public async Task RenamePlayer_InvalidOrTakenNickname_IsRefusedAndBroadcastsNothing(string nickname, RenamePlayerRefusal expected)
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);
        var zoeId = await JoinAsync(zoe, "Zoé");
        await JoinAsync(max, "Max");
        await FlushAsync(gameMaster);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        var state = Game.State;

        // When
        var result = await RenameAsync(gameMaster, zoeId, nickname);

        // Then
        Assert.Equal(new RenamePlayerResult(expected), result);
        Assert.Same(state, Game.State);
        await FlushAsync(gameMaster);
        Assert.Empty(toGameMaster.Json);
    }

    [Fact]
    public async Task RenamePlayer_UnknownPlayer_IsRefused()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        var state = Game.State;

        // When
        var result = await RenameAsync(gameMaster, new PlayerId(Guid.NewGuid()), "Max");

        // Then
        Assert.Equal(new RenamePlayerResult(RenamePlayerRefusal.PlayerUnknown), result);
        Assert.Same(state, Game.State);
    }

    [Fact]
    public async Task RenamePlayer_ToOwnNickname_IsAcceptedWithoutNewVersion()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        var zoeId = await JoinAsync(zoe, "Zoé");
        var state = Game.State;

        // When
        var result = await RenameAsync(gameMaster, zoeId, " Zoé ");

        // Then
        Assert.Equal(new RenamePlayerResult(Refusal: null), result);
        Assert.Same(state, Game.State);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Role.Display)]
    public async Task RenamePlayer_NotAuthenticatedAsGameMaster_IsIgnored(Role? role)
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        if (role is { } announced)
        {
            await AnnounceAsync(connection, announced);
        }

        var zoeId = await JoinAsync(zoe, "Zoé");
        var state = Game.State;

        // When
        var result = await RenameAsync(connection, zoeId, "Max");

        // Then
        Assert.Null(result);
        Assert.Same(state, Game.State);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("not authenticated as game master", StringComparison.Ordinal));
    }

    public static TheoryData<string, string> MalformedRequests => new()
    {
        { "{}", "$" },
        { """{ "playerId": "00000001-0000-0000-0000-000000000000" }""", "$" },
        { """{ "playerId": "not a guid", "nickname": "Max" }""", "$.playerId" },
        { """{ "playerId": "00000001-0000-0000-0000-000000000000", "nickname": null }""", "$.nickname" },
        { "null", "$" },
    };

    [Theory]
    [MemberData(nameof(MalformedRequests))]
    public async Task RenamePlayer_MalformedMessage_IsRefusedAndLogsWarning(string message, string expectedPath)
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        using var json = JsonDocument.Parse(message);
        var state = Game.State;

        // When
        var result = await gameMaster.InvokeAsync<RenamePlayerResult>(GameHub.RenamePlayer, json.RootElement, Ct);

        // Then
        Assert.Equal(new RenamePlayerResult(RenamePlayerRefusal.MessageInvalid), result);
        Assert.Same(state, Game.State);
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Malformed", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
        Assert.Contains($"\"JsonPath\":\"{expectedPath}\"", warning.Line, StringComparison.Ordinal);
    }

    private static T Latest<T>(IReadOnlyList<T> snapshots) => snapshots[^1];

    private async Task<HubConnection> ConnectGameMasterAsync()
    {
        var connection = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(connection, Role.GameMaster, Code);
        return connection;
    }

    private static async Task AnnounceAsync(HubConnection connection, Role role, string? code = null)
    {
        var result = await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(role, code), Ct);
        Assert.Null(result.Refusal);
    }

    private static async Task<PlayerId> JoinAsync(HubConnection connection, string nickname)
    {
        var result = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        Assert.Null(result.Refusal);
        return result.PlayerId!.Value;
    }

    /// <summary>
    /// Renames through the hub. The answer is <see langword="null"/> when the hub ignores the intent of a connection that
    /// is not authenticated as game master.
    /// </summary>
    private static Task<RenamePlayerResult?> RenameAsync(HubConnection connection, PlayerId playerId, string nickname) =>
        connection.InvokeAsync<RenamePlayerResult?>(GameHub.RenamePlayer, new RenamePlayerRequest(playerId, nickname), Ct);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
