using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

public sealed class ResumeSessionTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public ResumeSessionTests()
    {
        // The only pack of the directory, chosen at once, so that the game can start.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Manche"));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("LogFiles:Directory", _logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory.Services.GetRequiredService<GameLoop>();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
        _packs.Dispose();
    }

    [Fact]
    public async Task ResumeSession_KnownTokenAfterDisconnection_IdentifiesTheConnectionAndShowsThePlayerConnected()
    {
        // Given
        await using var display = await AnnounceAsync(Role.Display);
        await using var gameMaster = await AnnounceAsync(Role.GameMaster);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        var (playerId, token) = await JoinAndLeaveAsync("Zoé");
        await using var phone = await HubClients.ConnectAsync(_factory);
        using var toPhone = new ReceivedSnapshots(phone);
        var version = Game.State.Version;

        // When
        var result = await ResumeAsync(phone, token);

        // Then
        Assert.Equal(new ResumeSessionResult(Refusal: null, playerId), result);
        await WaitUntilAsync(() => Game.State.Version > version);
        Assert.True(Assert.Single(Game.State.Players).IsConnected);
        Assert.Equal([HubGroups.Player(playerId)], await GroupsOfAsync(phone, playerId));
        await Task.WhenAll(FlushAsync(phone), FlushAsync(display), FlushAsync(gameMaster));
        Assert.Equal(
            new PlayerSnapshot(Game.State.GameId, Game.State.Version, Phase.Lobby, playerId, "Zoé", Score: 0, PlayerCount: 1, Round: null, RoundView: null, Standing: null),
            toPhone.Player.MaxBy(s => s.Version));
        Assert.True(toDisplay.Display.MaxBy(s => s.Version)!.Players.Single().IsConnected);
        Assert.True(toGameMaster.GameMaster.MaxBy(s => s.Version)!.Players.Single().IsConnected);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("resumed their session", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResumeSession_AfterTheGameStarted_SendsTheCurrentPhase()
    {
        // Given
        await using var other = await HubClients.ConnectAsync(_factory);
        await other.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Max"), Ct);
        var (playerId, token) = await JoinAndLeaveAsync("Zoé");
        await using var gameMaster = await AnnounceAsync(Role.GameMaster);
        var started = await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        Assert.Null(started.Refusal);
        await using var phone = await HubClients.ConnectAsync(_factory);
        using var toPhone = new ReceivedSnapshots(phone);

        // When
        var result = await ResumeAsync(phone, token);

        // Then
        Assert.Equal(playerId, result.PlayerId);
        await FlushAsync(phone);
        // The first question of the only round of the pack is presented.
        var current = toPhone.Player.MaxBy(s => s.Version)!;
        Assert.Equal(Phase.Round, current.Phase);
        Assert.IsType<QuizPlayerView>(current.RoundView);
    }

    [Fact]
    public async Task ResumeSession_WhileAnotherConnectionIsOpen_KeepsThePlayerConnectedUntilBothClose()
    {
        // Given
        var firstTab = await HubClients.ConnectAsync(_factory);
        var token = (await firstTab.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct)).Token!;
        var secondTab = await HubClients.ConnectAsync(_factory);
        await ResumeAsync(secondTab, token);
        var version = Game.State.Version;

        // When
        await firstTab.DisposeAsync();
        await ProbeQueueAsync();

        // Then
        Assert.Equal(version, Game.State.Version);
        Assert.True(Assert.Single(Game.State.Players).IsConnected);

        // When
        await secondTab.DisposeAsync();

        // Then
        await WaitUntilAsync(() => Game.State.Version > version);
        Assert.False(Assert.Single(Game.State.Players).IsConnected);
    }

    [Fact]
    public async Task ResumeSession_ManyTimesInARow_EndsWithThePlayerConnected()
    {
        // Given
        var (_, token) = await JoinAndLeaveAsync("Zoé");

        // When
        for (var i = 0; i < 5; i++)
        {
            await using var phone = await HubClients.ConnectAsync(_factory);
            await ResumeAsync(phone, token);
        }

        await using var last = await HubClients.ConnectAsync(_factory);
        await ResumeAsync(last, token);
        await ProbeQueueAsync();

        // Then
        Assert.True(Assert.Single(Game.State.Players).IsConnected);
    }

    [Theory]
    [InlineData("unknown-token-from-another-evening")]
    [InlineData("")]
    public async Task ResumeSession_UnknownToken_IsRefusedWithoutJoiningAGroup(string token)
    {
        // Given
        await JoinAndLeaveAsync("Zoé");
        await using var phone = await HubClients.ConnectAsync(_factory);
        using var toPhone = new ReceivedSnapshots(phone);
        var version = Game.State.Version;

        // When
        var result = await ResumeAsync(phone, token);

        // Then
        Assert.Equal(new ResumeSessionResult(ResumeSessionRefusal.SessionUnknown, PlayerId: null), result);
        Assert.Equal(version, Game.State.Version);
        await FlushAsync(phone);
        Assert.Empty(toPhone.Json);

        // The phone may register instead, on the same connection.
        var joined = await phone.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Max"), Ct);
        Assert.Null(joined.Refusal);
    }

    [Fact]
    public async Task ResumeSession_OnAConnectionThatAlreadyIdentified_IsRefused()
    {
        // Given
        await using var phone = await HubClients.ConnectAsync(_factory);
        var token = (await phone.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct)).Token!;

        // When
        var result = await ResumeAsync(phone, token);
        var joined = await phone.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Max"), Ct);

        // Then
        Assert.Equal(ResumeSessionRefusal.AlreadyIdentified, result.Refusal);
        Assert.Equal(JoinRefusal.AlreadyJoined, joined.Refusal);
        Assert.Single(Game.State.Players);
    }

    public static TheoryData<string, string> MalformedRequests => new()
    {
        { "{}", "$" },
        { """{ "token": null }""", "$.token" },
        { """{ "token": 42 }""", "$.token" },
        { "\"token\"", "$" },
        { "null", "$" },
    };

    [Theory]
    [MemberData(nameof(MalformedRequests))]
    public async Task ResumeSession_MalformedMessage_IsRefusedAndLogsWarning(string message, string expectedPath)
    {
        // Given
        await using var phone = await HubClients.ConnectAsync(_factory);
        using var json = JsonDocument.Parse(message);

        // When
        var result = await phone.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, json.RootElement, Ct);

        // Then
        Assert.Equal(ResumeSessionRefusal.MessageInvalid, result.Refusal);
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Malformed", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
        Assert.Contains($"\"JsonPath\":\"{expectedPath}\"", warning.Line, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResumeSession_Token_AppearsInNoSnapshotAndNoLog()
    {
        // Given
        await using var display = await AnnounceAsync(Role.Display);
        using var toDisplay = new ReceivedSnapshots(display);
        var (_, token) = await JoinAndLeaveAsync("Zoé");
        await using var phone = await HubClients.ConnectAsync(_factory);
        await using var stranger = await HubClients.ConnectAsync(_factory);
        using var toPhone = new ReceivedSnapshots(phone);

        // When
        await ResumeAsync(phone, token);
        await ResumeAsync(stranger, "not-a-token-of-this-game");

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(phone));
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, new Secret(token, Audience.Everyone));
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toPhone.Json, new Secret(token, Audience.Everyone));
        var logs = _logs.ReadAllLogs();
        Assert.Contains("resumed their session", logs, StringComparison.Ordinal);
        Assert.DoesNotContain(token, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("not-a-token-of-this-game", logs, StringComparison.Ordinal);
    }

    /// <summary>Registers a player on a connection that then closes, as a phone does when it goes to sleep.</summary>
    private async Task<(PlayerId PlayerId, string Token)> JoinAndLeaveAsync(string nickname)
    {
        var phone = await HubClients.ConnectAsync(_factory);
        var result = await phone.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        var version = Game.State.Version;
        await phone.DisposeAsync();
        await WaitUntilAsync(() => Game.State.Version > version);
        return (result.PlayerId!.Value, result.Token!);
    }

    private async Task<HubConnection> AnnounceAsync(Role role)
    {
        var connection = await HubClients.ConnectAsync(_factory);
        var result = await connection.InvokeAsync<AnnouncementResult>(
            GameHub.Announce, new Announcement(role, role == Role.GameMaster ? Code : null), Ct);
        Assert.Null(result.Refusal);
        return connection;
    }

    private static Task<ResumeSessionResult> ResumeAsync(HubConnection connection, string token) =>
        connection.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, new ResumeSessionRequest(token), Ct);

    /// <summary>
    /// Waits until the loop has handled every input queued so far, including those of a disconnection: a registration
    /// queued now is answered after them.
    /// </summary>
    private async Task ProbeQueueAsync()
    {
        await using var probe = await HubClients.ConnectAsync(_factory);
        var refused = await probe.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(""), Ct);
        Assert.Equal(JoinRefusal.NicknameInvalid, refused.Refusal);
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
