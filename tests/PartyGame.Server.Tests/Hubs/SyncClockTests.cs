using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class SyncClockTests : IAsyncDisposable
{
    private const string Code = "482913";

    private static readonly DateTimeOffset _start = new(2026, 10, 2, 20, 0, 0, 123, TimeSpan.Zero);

    private readonly TempDirectory _logs = new();
    private readonly FakeTimeProvider _time = new(_start);
    private readonly WebApplicationFactory<Program> _factory;

    public SyncClockTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("LogFiles:Directory", _logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .ConfigureTestServices(services => services.AddSingleton<TimeProvider>(_time)));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory.Services.GetRequiredService<GameLoop>();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
    }

    [Fact]
    public async Task SyncClock_AnonymousConnection_ReturnsTheTimeOfTheTimeProvider()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);

        // When
        var result = await SyncAsync(connection);

        // Then
        Assert.Equal(new ClockSyncResult(_start.ToUnixTimeMilliseconds()), result);
    }

    [Fact]
    public async Task SyncClock_AfterTimeAdvanced_ReturnsTheNewTime()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        await SyncAsync(connection);

        // When
        _time.Advance(TimeSpan.FromMilliseconds(1_500));
        var result = await SyncAsync(connection);

        // Then
        Assert.Equal(_start.ToUnixTimeMilliseconds() + 1_500, result.ServerTime);
    }

    [Theory]
    [InlineData(Role.Display)]
    [InlineData(Role.GameMaster)]
    [InlineData(Role.Player)]
    public async Task SyncClock_WhateverTheRole_ReturnsTheTimeOfTheTimeProvider(Role role)
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        await IdentifyAsync(connection, role);

        // When
        var result = await SyncAsync(connection);

        // Then
        Assert.Equal(_start.ToUnixTimeMilliseconds(), result.ServerTime);
    }

    [Fact]
    public async Task SyncClock_OnTheWire_IsAnIntegerNumberOfMilliseconds()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);

        // When
        var result = await connection.InvokeAsync<JsonElement>(GameHub.SyncClock, Ct);

        // Then
        Assert.Equal(_start.ToUnixTimeMilliseconds(), result.GetProperty("serverTime").GetInt64());
    }

    [Fact]
    public async Task SyncClock_ABurst_LeavesTheStateUnchangedAndBroadcastsNothing()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await IdentifyAsync(display, Role.Display);
        await FlushAsync(display);
        using var toDisplay = new ReceivedSnapshots(display);
        var state = Game.State;

        // When
        for (var i = 0; i < 8; i++)
        {
            await SyncAsync(display);
        }

        // Then
        Assert.Same(state, Game.State);
        await FlushAsync(display);
        Assert.Empty(toDisplay.Json);
    }

    [Fact]
    public async Task SyncClock_ABurst_LogsNothing()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        var logged = LoggedEvent.ReadAll(_logs).Count;

        // When
        for (var i = 0; i < 8; i++)
        {
            await SyncAsync(connection);
        }

        // Then: an announcement logged afterwards is the only new event, so nothing was pending before it.
        await IdentifyAsync(connection, Role.Display);
        var events = LoggedEvent.ReadAll(_logs).Skip(logged);
        Assert.Equal("Connection {ConnectionId} announced as {Role}", Assert.Single(events).Template);
    }

    private static Task<ClockSyncResult> SyncAsync(HubConnection connection) =>
        connection.InvokeAsync<ClockSyncResult>(GameHub.SyncClock, Ct);

    private static async Task IdentifyAsync(HubConnection connection, Role role)
    {
        if (role == Role.Player)
        {
            var joined = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);
            Assert.Null(joined.Refusal);
            return;
        }

        var announced = await connection.InvokeAsync<AnnouncementResult>(
            GameHub.Announce,
            new Announcement(role, role == Role.GameMaster ? Code : null),
            Ct);
        Assert.Null(announced.Refusal);
    }

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
