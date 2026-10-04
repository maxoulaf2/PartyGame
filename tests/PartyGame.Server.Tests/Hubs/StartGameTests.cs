using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class StartGameTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public StartGameTests()
    {
        // The only pack of the directory, chosen at once, so that the game can start.
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
    public async Task StartGame_AsGameMasterWithPlayers_PresentsTheFirstQuestionOnEveryInterface()
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

        // When
        var result = await StartAsync(gameMaster);

        // Then
        Assert.Equal(new StartGameResult(Refusal: null), result);
        // The first question of the single round of the pack is presented on every interface, its answer to the game
        // master only. The TV screen shows nothing of it until the game master reads it out.
        Assert.Equal(GamePhase.Round, Game.State.Phase);
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        var toTheDisplay = Assert.IsType<QuizDisplayView>(toDisplay.Display[^1].RoundView);
        var toTheGameMaster = Assert.IsType<QuizGameMasterView>(toGameMaster.GameMaster[^1].RoundView);
        var toThePlayer = Assert.IsType<QuizPlayerView>(toZoe.Player[^1].RoundView);
        Assert.Equal((Phase.Round, QuizQuestionPhase.Presentation, null, 2), (toDisplay.Display[^1].Phase, toTheDisplay.Phase, toTheDisplay.Text, toTheDisplay.ChoiceCount));
        Assert.Equal(("Question ?", false), (toTheGameMaster.Text, toTheGameMaster.QuestionShown));
        Assert.Equal((Phase.Round, QuizQuestionPhase.Presentation), (toZoe.Player[^1].Phase, toThePlayer.Phase));
        Assert.Equal([QuizChoiceLetter.A, QuizChoiceLetter.B], toThePlayer.Choices);
        Assert.Equal(
            [
                new QuizGameMasterChoice(QuizChoiceLetter.A, "Oui", Correct: true, Shown: false, AnswerCount: 0),
                new QuizGameMasterChoice(QuizChoiceLetter.B, "Non", Correct: false, Shown: false, AnswerCount: 0),
            ],
            toTheGameMaster.Choices);
        Assert.Empty(toTheDisplay.Choices);
        Assert.DoesNotContain("correct", toDisplay.Json[^1], StringComparison.OrdinalIgnoreCase);
        // The phone has a place for the correct choice and the verdict, both empty until the reveal.
        Assert.Equal((null, null), (toThePlayer.CorrectChoice, toThePlayer.Verdict));
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Game started", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartGame_WithoutPlayers_IsRefusedAndBroadcastsNothing()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await FlushAsync(gameMaster);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        var state = Game.State;

        // When
        var result = await StartAsync(gameMaster);

        // Then
        Assert.Equal(new StartGameResult(StartGameRefusal.NotEnoughPlayers), result);
        Assert.Same(state, Game.State);
        await FlushAsync(gameMaster);
        Assert.Empty(toGameMaster.Json);
    }

    [Fact]
    public async Task StartGame_AlreadyStarted_IsRefusedAndBroadcastsNothing()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        Assert.Null((await StartAsync(gameMaster))?.Refusal);
        await FlushAsync(secondGameMaster);
        using var toGameMaster = new ReceivedSnapshots(secondGameMaster);
        var state = Game.State;

        // When
        var result = await StartAsync(secondGameMaster);

        // Then
        Assert.Equal(new StartGameResult(StartGameRefusal.AlreadyStarted), result);
        Assert.Same(state, Game.State);
        await FlushAsync(secondGameMaster);
        Assert.Empty(toGameMaster.Json);
    }

    [Fact]
    public async Task StartGame_TwoAtOnce_StartsTheGameOnce()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        var version = Game.State.Version;

        // When
        var results = await Task.WhenAll(StartAsync(gameMaster), StartAsync(gameMaster));

        // Then
        Assert.Equal(
            [null, StartGameRefusal.AlreadyStarted],
            results.Select(r => r?.Refusal).Order());
        Assert.Equal(version + 1, Game.State.Version);
    }

    [Fact]
    public async Task JoinGame_AfterStart_RegistersThePlayerWhoSeesTheStartedGame()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        Assert.Null((await StartAsync(gameMaster))?.Refusal);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toMax = new ReceivedSnapshots(max);

        // When
        await JoinAsync(max, "Max");

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(max));
        // Joined during the presentation of the first question, Max sees it like everybody else.
        Assert.Equal((Phase.Round, "Max"), (toMax.Player[^1].Phase, toMax.Player[^1].Nickname));
        Assert.Equal(QuizQuestionPhase.Presentation, Assert.IsType<QuizPlayerView>(toMax.Player[^1].RoundView).Phase);
        Assert.Equal(["Zoé", "Max"], toDisplay.Display[^1].Players.Select(p => p.Nickname));
        Assert.Equal(["Zoé", "Max"], toGameMaster.GameMaster[^1].Players.Select(p => p.Nickname));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Role.Display)]
    public async Task StartGame_NotAuthenticatedAsGameMaster_IsIgnored(Role? role)
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        if (role is { } announced)
        {
            await AnnounceAsync(connection, announced);
        }

        await JoinAsync(zoe, "Zoé");
        var state = Game.State;

        // When
        var result = await StartAsync(connection);

        // Then
        Assert.Null(result);
        Assert.Same(state, Game.State);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("not authenticated as game master", StringComparison.Ordinal));
    }

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

    /// <summary>
    /// Starts the game through the hub. The answer is <see langword="null"/> when the hub ignores the intent of a
    /// connection that is not authenticated as game master.
    /// </summary>
    private static Task<StartGameResult?> StartAsync(HubConnection connection) =>
        connection.InvokeAsync<StartGameResult?>(GameHub.StartGame, Ct);

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
