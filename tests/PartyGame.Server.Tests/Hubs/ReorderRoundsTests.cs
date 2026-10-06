using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Persistence;
using PartyGame.Server.Tests.Packs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Change of the programme by the game master through the hub, during the first round of a quiz of three rounds.
/// </summary>
public sealed class ReorderRoundsTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _data = new();
    private readonly TempDirectory _packs = new();
    private WebApplicationFactory<Program> _factory;

    public ReorderRoundsTests()
    {
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Échauffement", "Intermède", "Finale"));
        _factory = CreateFactory();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory.Services.GetRequiredService<GameLoop>();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _data.Dispose();
        _packs.Dispose();
    }

    [Fact]
    public async Task ReorderRounds_DuringARound_ShowsTheProgrammeToTheGameMasterAloneAndLogsIt()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var display = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display, code: null);
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        await FlushAsync(gameMaster, display, zoe);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toZoe = new ReceivedSnapshots(zoe);

        // When: the last round first, the second withdrawn
        await ReorderAsync(gameMaster, [new(1, false), new(2, false)], [new(2, false), new(1, true)]);
        await FlushAsync(gameMaster, display, zoe);

        // Then
        Assert.Equal(
            [("Échauffement", ScheduledRoundStatus.Current), ("Finale", ScheduledRoundStatus.Upcoming), ("Intermède", ScheduledRoundStatus.Withdrawn)],
            toGameMaster.GameMaster[^1].Schedule.Select(r => (r.Title, r.Status)));
        Assert.Equal((1, 2), (toDisplay.Display[^1].Round!.Number, toDisplay.Display[^1].Round!.Count));
        Assert.Equal(2, toZoe.Player[^1].Round!.Count);
        Secret[] programme = [new("Intermède", Audience.AllButGameMaster), new("Finale", Audience.AllButGameMaster)];
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, programme);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Json, programme);
        Assert.Contains(
            LoggedEvent.ReadAll(_data),
            e => e.Level == "Information" && e.Template.Contains("changed by the game master", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReorderRounds_TwiceAtOnceFromTheSameProgramme_AppliesOnlyOne()
    {
        // Given: two game master consoles that both see the order of the pack
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var version = Game.State.Version;

        // When
        await Task.WhenAll(
            ReorderAsync(gameMaster, [new(1, false), new(2, false)], [new(2, false), new(1, false)]),
            ReorderAsync(secondGameMaster, [new(1, false), new(2, false)], [new(1, true), new(2, false)]));

        // Then
        Assert.Equal(version + 1, Game.State.Version);
    }

    [Fact]
    public async Task ReorderRounds_NotAuthenticatedAsGameMaster_IsIgnored()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var state = Game.State;

        // When
        await ReorderAsync(zoe, [new(1, false), new(2, false)], [new(2, false), new(1, true)]);

        // Then
        Assert.Same(state, Game.State);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"gameId":"pas-un-guid","expectedOrder":[],"newOrder":[]}""")]
    [InlineData("null")]
    public async Task ReorderRounds_Malformed_IsIgnoredWithAWarning(string json)
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var state = Game.State;

        // When
        await gameMaster.InvokeAsync(GameHub.ReorderRounds, JsonDocument.Parse(json).RootElement, Ct);

        // Then
        Assert.Same(state, Game.State);
        Assert.Contains(
            LoggedEvent.ReadAll(_data),
            e => e.Level == "Warning" && e.Template.StartsWith("Malformed", StringComparison.Ordinal)
                && e.Line.Contains(GameHub.ReorderRounds, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Startup_GameSavedBeforeTheProgrammeExisted_FollowsTheOrderOfThePack()
    {
        // Given: a game saved in its first round, without the programme
        await using (var gameMaster = await ConnectGameMasterAsync())
        await using (var zoe = await JoinAsync("Zoé"))
        {
            await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        }

        await _factory.DisposeAsync();
        var file = Path.Combine(_data.Path, GamePersistence.FileName);
        var json = JsonNode.Parse(await File.ReadAllTextAsync(file, Ct))!;
        Assert.True(json["game"]!.AsObject().Remove("schedule"));
        await File.WriteAllTextAsync(file, json.ToJsonString(), Ct);

        // When
        _factory = CreateFactory();

        // Then
        var schedule = Game.State.PendingGame!.Game.Schedule;
        Assert.Equal((ImmutableArray<int>)[1, 2], schedule.Upcoming);
        Assert.Empty(schedule.Past);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_data.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path));

    private async Task<HubConnection> ConnectGameMasterAsync()
    {
        var connection = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(connection, Role.GameMaster, Code);
        return connection;
    }

    private static async Task AnnounceAsync(HubConnection connection, Role role, string? code) =>
        Assert.Null((await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(role, code), Ct)).Refusal);

    private async Task<HubConnection> JoinAsync(string nickname)
    {
        var connection = await HubClients.ConnectAsync(_factory);
        var result = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        Assert.Null(result.Refusal);
        return connection;
    }

    private Task ReorderAsync(HubConnection connection, ImmutableArray<ScheduledRound> expected, ImmutableArray<ScheduledRound> order) =>
        connection.InvokeAsync(
            GameHub.ReorderRounds,
            JsonSerializer.SerializeToElement(new ReorderRoundsRequest(Game.State.GameId, expected, order), ContractJsonOptions.Default),
            Ct);

    private async Task FlushAsync(params HubConnection[] connections)
    {
        foreach (var connection in connections)
        {
            await HubClients.FlushAsync<GameHub>(_factory, connection);
        }
    }
}
