using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine;
using PartyGame.Engine.Modes;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Incidents;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

public sealed class IncidentsTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public IncidentsTests()
    {
        // The only pack of the directory, chosen at once: its rounds are played by a mode whose player intents all throw.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Échauffement", "Finale"));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path)
            .ConfigureTestServices(services =>
            {
                services.RemoveAll<IGameMode>();
                services.AddSingleton<IGameMode>(new FaultyQuizMode());
            }));
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
    public async Task RoundIntent_ModeThrows_KeepsTheStateAndTellsTheGameMasterOnly()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        var started = Game.State;
        var counts = (toDisplay.Messages.Count, toGameMaster.Messages.Count, toZoe.Messages.Count);

        // When
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, new QuizSubmitAnswer(started.CurrentRound!.Id, 1, QuizChoiceLetter.A));

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        Assert.Same(started, Game.State);
        var incident = Assert.Single(Assert.Single(toGameMaster.Incidents).Incidents);
        Assert.Equal(
            (IncidentCode.RoundHandlerFailed, new RoundInfo(started.CurrentRound.Id, Number: 1, Count: 2, "Échauffement"), (Role?)null, 1),
            (incident.Code, incident.Round, incident.Role, incident.Count));
        Assert.Equal(counts with { Item2 = counts.Item2 + 1 }, (toDisplay.Messages.Count, toGameMaster.Messages.Count, toZoe.Messages.Count));
        Assert.Equal(started.Version, toGameMaster.GameMaster[^1].Version);
        var secrets = new Secret(nameof(IncidentCode.RoundHandlerFailed), Audience.AllButGameMaster);
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Messages, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Messages, secrets);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Level == "Error" && e.Template.StartsWith("Input {InputType} failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Announce_GameMasterAfterIncidents_ReceivesEveryIncidentAtOnce()
    {
        // Given: two failures in the round, while a first console is connected
        await using var firstConsole = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await firstConsole.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var round = Game.State.CurrentRound!.Id;
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, new QuizSubmitAnswer(round, 1, QuizChoiceLetter.A));
        await PlayerIntents.SendAsync(zoe, clientSeq: 2, new QuizSubmitAnswer(round, 1, QuizChoiceLetter.B));

        // When: another console comes, as one coming back after a reload
        await using var secondConsole = await HubClients.ConnectAsync(_factory);
        using var toSecondConsole = new ReceivedSnapshots(secondConsole);
        await AnnounceAsync(secondConsole, Role.GameMaster, Code);

        // Then
        await FlushAsync(secondConsole);
        var list = Assert.Single(toSecondConsole.Incidents);
        Assert.Equal(2, list.Version);
        var incident = Assert.Single(list.Incidents);
        Assert.Equal((IncidentCode.RoundHandlerFailed, round, 2), (incident.Code, incident.Round?.RoundId, incident.Count));
    }

    [Fact]
    public async Task Announce_Display_ReceivesNoIncident()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, new QuizSubmitAnswer(Game.State.CurrentRound!.Id, 1, QuizChoiceLetter.A));

        // When
        await using var display = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        await AnnounceAsync(display, Role.Display);

        // Then
        await FlushAsync(display);
        Assert.Empty(toDisplay.Incidents);
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Messages, new Secret(nameof(IncidentCode.RoundHandlerFailed), Audience.AllButGameMaster));
    }

    [Fact]
    public async Task SkipRound_RoundFailingRepeatedly_IsOfferedThenEndsWithoutAnyMessage()
    {
        // Given: every input of a player fails in the first round
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var first = Game.State.CurrentRound!.Id;
        for (var seq = 1; seq <= IncidentJournal.SkipThreshold; seq++)
        {
            await FlushAsync(gameMaster);
            Assert.All(toGameMaster.Incidents, list => Assert.Empty(list.FailingRounds));
            await PlayerIntents.SendAsync(zoe, seq, new QuizSubmitAnswer(first, 1, QuizChoiceLetter.A));
        }

        await FlushAsync(gameMaster);
        Assert.Equal([first], toGameMaster.Incidents[^1].FailingRounds);

        // When
        await gameMaster.InvokeAsync(GameHub.SkipRound, Message(new SkipRoundRequest(first)), Ct);

        // Then: the TV screen and the phone show the usual end of the round
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        Assert.Equal(GamePhase.BetweenRounds, Game.State.Phase);
        Assert.Equal(Phase.BetweenRounds, toDisplay.Display[^1].Phase);
        Assert.Equal(Phase.BetweenRounds, toZoe.Player[^1].Phase);
        Secret[] secrets =
        [
            new(nameof(IncidentCode.RoundHandlerFailed), Audience.AllButGameMaster),
            new(nameof(IncidentList.FailingRounds), Audience.AllButGameMaster),
            new(nameof(GameMasterSnapshot.RoundSkipped), Audience.AllButGameMaster),
        ];
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Messages, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Messages, secrets);
    }

    [Fact]
    public async Task RoundIntent_NextRoundFailing_CountsItsFailuresFromZero()
    {
        // Given: the first round skipped once it kept failing, then the next one started
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var first = Game.State.CurrentRound!.Id;
        var seq = 0;
        for (var i = 0; i < IncidentJournal.SkipThreshold; i++)
        {
            await PlayerIntents.SendAsync(zoe, ++seq, new QuizSubmitAnswer(first, 1, QuizChoiceLetter.A));
        }

        await gameMaster.InvokeAsync(GameHub.SkipRound, Message(new SkipRoundRequest(first)), Ct);
        await gameMaster.InvokeAsync(GameHub.NextRound, Message(new NextRoundRequest(first)), Ct);
        var final = Game.State.CurrentRound!.Id;

        // When
        await PlayerIntents.SendAsync(zoe, ++seq, new QuizSubmitAnswer(final, 1, QuizChoiceLetter.A));

        // Then: the final is not offered to be skipped after a single failure
        await FlushAsync(gameMaster);
        Assert.Equal(GamePhase.Round, Game.State.Phase);
        Assert.Equal([first], toGameMaster.Incidents[^1].FailingRounds);
    }

    private static JsonElement Message<T>(T message) => JsonSerializer.SerializeToElement(message, ContractJsonOptions.Default);

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

    private static async Task JoinAsync(HubConnection connection, string nickname)
    {
        var result = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        Assert.Null(result.Refusal);
    }

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
