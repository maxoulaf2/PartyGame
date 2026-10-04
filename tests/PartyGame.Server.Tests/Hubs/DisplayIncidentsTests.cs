using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

public sealed class DisplayIncidentsTests : IAsyncDisposable
{
    private const string Code = "482913";

    private const string Flag = "images/drapeau-japon.png";

    private const string Monument = "monuments/tour-eiffel.webp";

    private const string MediaFailedTemplate = "TV screen could not load media {MediaPath} ({MediaId}), round {RoundNumber}, step {Step}";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public DisplayIncidentsTests()
    {
        // The only pack of the directory, chosen at once: a quiz round whose two questions each have an image.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.IllustratedQuiz("Soirée illustrée", Flag, Monument));
        TestPacks.WriteMedia(_packs.Path, "soiree", Flag, [1, 2, 3]);
        TestPacks.WriteMedia(_packs.Path, "soiree", Monument, [4, 5, 6]);
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
    public async Task ReportClientError_RenderFailedOnTheDisplay_OffersTheGameMasterToSkipTheRound()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        var started = Game.State;
        var counts = (toDisplay.Messages.Count, toGameMaster.Messages.Count, toZoe.Messages.Count);

        // When
        await display.InvokeAsync(GameHub.ReportClientError, Report(Role.Display, ClientErrorKind.RenderFailed), Ct);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        Assert.Same(started, Game.State);
        var list = Assert.Single(toGameMaster.Incidents);
        var incident = Assert.Single(list.Incidents);
        Assert.Equal(
            (IncidentCode.DisplayViewFailed, started.CurrentRound!.Id, (Role?)null, (int?)null),
            (incident.Code, incident.Round?.RoundId, incident.Role, incident.Step));
        Assert.Equal([started.CurrentRound.Id], list.FailingRounds);
        Assert.Equal(counts with { Item2 = counts.Item2 + 1 }, (toDisplay.Messages.Count, toGameMaster.Messages.Count, toZoe.Messages.Count));
        Secret[] secrets =
        [
            new(nameof(IncidentCode.DisplayViewFailed), Audience.AllButGameMaster),
            new(nameof(IncidentList.FailingRounds), Audience.AllButGameMaster),
        ];
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Messages, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Messages, secrets);
    }

    [Fact]
    public async Task ReportClientError_RenderFailedOutsideARound_ReportsAnIncidentWithoutRound()
    {
        // Given: the lobby
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When
        await display.InvokeAsync(GameHub.ReportClientError, Report(Role.Display, ClientErrorKind.RenderFailed), Ct);

        // Then
        await FlushAsync(gameMaster);
        var list = Assert.Single(toGameMaster.Incidents);
        Assert.Equal((IncidentCode.DisplayViewFailed, (RoundInfo?)null), (Assert.Single(list.Incidents).Code, list.Incidents[0].Round));
        Assert.Empty(list.FailingRounds);
    }

    public static TheoryData<Role?, Role, ClientErrorKind> NoIncidentReports => new()
    {
        // The game master can do nothing for a phone, nor for the console it sees itself.
        { null, Role.Player, ClientErrorKind.RenderFailed },
        { Role.GameMaster, Role.GameMaster, ClientErrorKind.RenderFailed },

        // A page that claims to be the TV screen without having announced it.
        { null, Role.Display, ClientErrorKind.RenderFailed },

        // Errors outside a rendering are no view the public misses.
        { Role.Display, Role.Display, ClientErrorKind.Error },
        { Role.Display, Role.Display, ClientErrorKind.UnhandledRejection },
    };

    [Theory]
    [MemberData(nameof(NoIncidentReports))]
    public async Task ReportClientError_NotARenderingOfTheDisplay_IsOnlyLogged(Role? announced, Role reported, ClientErrorKind kind)
    {
        // Given: a round in progress
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await using var other = await HubClients.ConnectAsync(_factory);
        if (announced is { } role)
        {
            await AnnounceAsync(other, role, role == Role.GameMaster ? Code : null);
        }

        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await FlushAsync(gameMaster);
        Assert.Equal(GamePhase.Round, Game.State.Phase);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        var connection = reported == Role.Player ? zoe : other;

        // When
        await connection.InvokeAsync(GameHub.ReportClientError, Report(reported, kind), Ct);

        // Then
        await FlushAsync(gameMaster);
        Assert.Empty(toGameMaster.Incidents);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Level == "Warning" && e.Template.StartsWith("Client error", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(Flag, 1)]
    [InlineData(Monument, 2)]
    public async Task ReportDisplayMediaFailure_MediaOfTheRound_TellsTheGameMasterTheRoundAndTheQuestion(string media, int question)
    {
        // Given: the first question in progress
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        var started = Game.State;
        var counts = (toDisplay.Messages.Count, toGameMaster.Messages.Count, toZoe.Messages.Count);
        var id = IdOf(media);

        // When
        await display.InvokeAsync(GameHub.ReportDisplayMediaFailure, new DisplayMediaFailureReport(id), Ct);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        Assert.Same(started, Game.State);
        Assert.Equal(counts with { Item2 = counts.Item2 + 1 }, (toDisplay.Messages.Count, toGameMaster.Messages.Count, toZoe.Messages.Count));
        var list = Assert.Single(toGameMaster.Incidents);
        var incident = Assert.Single(list.Incidents);
        Assert.Equal(
            (IncidentCode.DisplayMediaFailed, new RoundInfo(started.CurrentRound!.Id, Number: 1, Count: 1, "Manche illustrée"), (int?)question),
            (incident.Code, incident.Round, incident.Step));
        Assert.Empty(list.FailingRounds);

        // The path of the file stays in the logs of the server: the console has no use for it, and no client sees it.
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template == MediaFailedTemplate);
        Assert.Equal("Warning", warning.Level);
        using var json = JsonDocument.Parse(warning.Line);
        Assert.Equal(media, json.RootElement.GetProperty("MediaPath").GetString());
        Assert.Equal(question, json.RootElement.GetProperty("Step").GetInt32());
        var secrets = new[]
        {
            new Secret(nameof(IncidentCode.DisplayMediaFailed), Audience.AllButGameMaster),
            new Secret(media, Audience.Everyone),
        };
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Messages, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Messages, secrets);
        LeakAssert.NoSecretReceived(Viewer.GameMaster, toGameMaster.Messages, secrets);
    }

    [Fact]
    public async Task ReportDisplayMediaFailure_SameMediaTwice_CountsItOnOneLine()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await FlushAsync(gameMaster);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        var report = new DisplayMediaFailureReport(IdOf(Flag));
        await display.InvokeAsync(GameHub.ReportDisplayMediaFailure, report, Ct);

        // When: the TV screen reloaded, and failed again
        await display.InvokeAsync(GameHub.ReportDisplayMediaFailure, report, Ct);

        // Then
        await FlushAsync(gameMaster);
        Assert.Equal(2, Assert.Single(toGameMaster.Incidents[^1].Incidents).Count);
    }

    [Fact]
    public async Task ReportDisplayMediaFailure_FromAnotherRole_IsIgnored()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await FlushAsync(gameMaster);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When
        await zoe.InvokeAsync(GameHub.ReportDisplayMediaFailure, new DisplayMediaFailureReport(IdOf(Flag)), Ct);

        // Then
        await FlushAsync(gameMaster);
        Assert.Empty(toGameMaster.Incidents);
        Assert.Contains(
            LoggedEvent.ReadAll(_logs),
            e => e.Level == "Warning" && e.Template == "{HubMethod} from connection {ConnectionId} ignored: it did not announce itself as the TV screen");
    }

    [Fact]
    public async Task ReportDisplayMediaFailure_UnknownMedia_IsIgnored()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await FlushAsync(gameMaster);
        Assert.Equal(GamePhase.Round, Game.State.Phase);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When: an identifier of another game
        await display.InvokeAsync(GameHub.ReportDisplayMediaFailure, new DisplayMediaFailureReport("AAAAAAAAAAAAAAAAAAAAAA"), Ct);

        // Then
        await FlushAsync(gameMaster);
        Assert.Empty(toGameMaster.Incidents);
        Assert.Contains(
            LoggedEvent.ReadAll(_logs),
            e => e.Level == "Warning" && e.Template == "Media failure from connection {ConnectionId} ignored: the game has no media {MediaId}");
    }

    public static TheoryData<string, string> MalformedReports => new()
    {
        { """{}""", "$" },
        { """{"mediaId":42}""", "$.mediaId" },
        { $$"""{"mediaId":"{{new string('a', 65)}}"}""", "$.mediaId" },
    };

    [Theory]
    [MemberData(nameof(MalformedReports))]
    public async Task ReportDisplayMediaFailure_MalformedMessage_IsIgnoredWithAWarning(string message, string invalidPath)
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When
        using var json = JsonDocument.Parse(message);
        await display.InvokeAsync(GameHub.ReportDisplayMediaFailure, json.RootElement, Ct);

        // Then
        await FlushAsync(gameMaster);
        Assert.Empty(toGameMaster.Incidents);
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Malformed {HubMethod}", StringComparison.Ordinal));
        using var properties = JsonDocument.Parse(warning.Line);
        Assert.Equal(GameHub.ReportDisplayMediaFailure, properties.RootElement.GetProperty("HubMethod").GetString());
        Assert.Equal(invalidPath, properties.RootElement.GetProperty("JsonPath").GetString());
    }

    private static ClientErrorReport Report(Role role, ClientErrorKind kind) =>
        new(role, "/display/", kind, "TypeError: Cannot read properties of null", Stack: null, "quiz", SnapshotVersion: 3, BuildId: null);

    private string IdOf(string media) => Game.State.Media.Files.Single(file => file.Value == new MediaPath(media)).Key.Value;

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
