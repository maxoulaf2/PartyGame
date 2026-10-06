using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Correction of a score by the game master through the hub, during the first round of a quiz.
/// </summary>
public sealed class AdjustScoreTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public AdjustScoreTests()
    {
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Manche"));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
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
    public async Task AdjustScore_DuringARound_ShowsTheNewTotalOnThePhoneAndLogsIt()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        await FlushAsync(zoe);
        using var toZoe = new ReceivedSnapshots(zoe);

        // When
        await AdjustAsync(gameMaster, expected: 0, now: 500);
        await FlushAsync(zoe);

        // Then
        Assert.Equal(500, toZoe.Player[^1].Score);
        Assert.Contains(
            LoggedEvent.ReadAll(_logs),
            e => e.Level == "Information" && e.Template.Contains("adjusted by the game master", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdjustScore_TwiceAtOnceFromTheSameScore_AppliesOnlyOne()
    {
        // Given: two game master consoles that both see 0
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var version = Game.State.Version;

        // When
        await Task.WhenAll(AdjustAsync(gameMaster, expected: 0, now: 500), AdjustAsync(secondGameMaster, expected: 0, now: 200));

        // Then
        Assert.True(Game.State.Players[0].Score is 500 or 200);
        Assert.Equal(version + 1, Game.State.Version);
    }

    [Fact]
    public async Task AdjustScore_NotAuthenticatedAsGameMaster_IsIgnored()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var state = Game.State;

        // When
        await AdjustAsync(zoe, expected: 0, now: 9999);

        // Then
        Assert.Same(state, Game.State);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"playerId":"pas-un-guid","expectedScore":0,"newScore":10}""")]
    [InlineData("null")]
    public async Task AdjustScore_Malformed_IsIgnoredWithAWarning(string json)
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var state = Game.State;

        // When
        await gameMaster.InvokeAsync(GameHub.AdjustScore, JsonDocument.Parse(json).RootElement, Ct);

        // Then
        Assert.Same(state, Game.State);
        Assert.Contains(
            LoggedEvent.ReadAll(_logs),
            e => e.Level == "Warning" && e.Template.StartsWith("Malformed", StringComparison.Ordinal)
                && e.Line.Contains(GameHub.AdjustScore, StringComparison.Ordinal));
    }

    private async Task<HubConnection> ConnectGameMasterAsync()
    {
        var connection = await HubClients.ConnectAsync(_factory);
        var result = await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(Role.GameMaster, Code), Ct);
        Assert.Null(result.Refusal);
        return connection;
    }

    private async Task<HubConnection> JoinAsync(string nickname)
    {
        var connection = await HubClients.ConnectAsync(_factory);
        var result = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        Assert.Null(result.Refusal);
        return connection;
    }

    private Task AdjustAsync(HubConnection connection, int expected, int now) =>
        connection.InvokeAsync(
            GameHub.AdjustScore,
            JsonSerializer.SerializeToElement(new AdjustScoreRequest(Game.State.Players[0].Id, expected, now), ContractJsonOptions.Default),
            Ct);

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
