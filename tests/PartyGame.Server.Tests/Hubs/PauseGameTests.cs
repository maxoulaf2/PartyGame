using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Pause of the game by the game master through the hub, during the countdown of a quiz question, with a clock the test
/// drives.
/// </summary>
public sealed class PauseGameTests : IAsyncDisposable
{
    private const string Code = "482913";

    private static readonly DateTimeOffset _start = new(2026, 10, 3, 21, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly FakeTimeProvider _time = new(_start);
    private readonly PlayerIntents _playerIntents = new();
    private readonly WebApplicationFactory<Program> _factory;

    public PauseGameTests()
    {
        // The only pack of the directory, chosen at once: a single question, "Oui" (A) being its correct answer.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Manche"));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path)
            .ConfigureTestServices(services => services.AddSingleton<TimeProvider>(_time)));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory.Services.GetRequiredService<GameLoop>();

    private RoundId RoundId => Game.State.CurrentRound!.Id;

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
        _packs.Dispose();
    }

    [Fact]
    public async Task PauseGame_DuringTheCountdown_StopsItUntilTheGameMasterResumes()
    {
        // Given: the countdown runs, 8 s in
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        await PresentAsync(gameMaster);
        _time.Advance(TimeSpan.FromSeconds(8));
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);

        // When: paused for 5 minutes, during which Zoé's answer arrives
        await PauseAsync(gameMaster, paused: true);
        _time.Advance(TimeSpan.FromMinutes(5));
        await AnswerAsync(zoe, QuizChoiceLetter.A);
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));

        // Then: every interface shows the pause, and nothing else moved
        var pausedAt = _start.AddSeconds(8).ToUnixTimeMilliseconds();
        Assert.Equal(
            (pausedAt, pausedAt, pausedAt),
            (toDisplay.Display[^1].PausedAt, toGameMaster.GameMaster[^1].PausedAt, toZoe.Player[^1].PausedAt));
        var paused = Assert.IsType<QuizPlayerView>(toZoe.Player[^1].RoundView);
        Assert.Equal((QuizQuestionPhase.Answering, null), (paused.Phase, paused.Answer));

        // When: resumed
        var version = Game.State.Version;
        await PauseAsync(gameMaster, paused: false);
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe));

        // Then: 12 s left, as when paused
        var resumedAt = _time.GetUtcNow();
        var onDisplay = Assert.IsType<QuizDisplayView>(toDisplay.Display[^1].RoundView);
        Assert.Equal((null, resumedAt.AddSeconds(QuizRoundDescriptor.DefaultAnswerSeconds - 8).ToUnixTimeMilliseconds()), (toDisplay.Display[^1].PausedAt, onDisplay.AnswersCloseAt));

        // The countdown, scheduled again, runs out 12 s later
        _time.Advance(TimeSpan.FromSeconds(QuizRoundDescriptor.DefaultAnswerSeconds - 8));
        await WaitUntilAsync(() => Game.State.Version > version + 1);
        await FlushAsync(display);
        Assert.Equal(QuizQuestionPhase.Locked, Assert.IsType<QuizDisplayView>(toDisplay.Display[^1].RoundView).Phase);
    }

    [Fact]
    public async Task PauseGame_TwiceAtOnce_PausesOnce()
    {
        // Given: two game master consoles during the first round
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var version = Game.State.Version;

        // When
        await Task.WhenAll(PauseAsync(gameMaster, paused: true), PauseAsync(secondGameMaster, paused: true));

        // Then
        Assert.NotNull(Game.State.PausedAt);
        Assert.Equal(version + 1, Game.State.Version);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Level == "Information" && e.Template.Contains("paused by the game master", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PauseGame_NotAuthenticatedAsGameMaster_IsIgnored()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var state = Game.State;

        // When
        await PauseAsync(zoe, paused: true);

        // Then
        Assert.Same(state, Game.State);
    }

    [Fact]
    public async Task JoinGame_WhilePaused_ReceivesTheGamePaused()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        await PauseAsync(gameMaster, paused: true);

        // When
        await using var max = await HubClients.ConnectAsync(_factory);
        using var toMax = new ReceivedSnapshots(max);
        Assert.Null((await max.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Max"), Ct)).Refusal);
        await FlushAsync(max);

        // Then
        Assert.Equal(_start.ToUnixTimeMilliseconds(), toMax.Player[^1].PausedAt);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"gameId":"pas-un-guid","paused":true}""")]
    [InlineData("null")]
    public async Task PauseGame_Malformed_IsIgnoredWithAWarning(string json)
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var state = Game.State;

        // When
        await gameMaster.InvokeAsync(GameHub.PauseGame, JsonDocument.Parse(json).RootElement, Ct);

        // Then
        Assert.Same(state, Game.State);
        Assert.Contains(
            LoggedEvent.ReadAll(_logs),
            e => e.Level == "Warning" && e.Template.StartsWith("Malformed", StringComparison.Ordinal)
                && e.Line.Contains(GameHub.PauseGame, StringComparison.Ordinal));
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

    private async Task<HubConnection> JoinAsync(string nickname)
    {
        var connection = await HubClients.ConnectAsync(_factory);
        var result = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        Assert.Null(result.Refusal);
        return connection;
    }

    private Task PauseAsync(HubConnection connection, bool paused) =>
        connection.InvokeAsync(GameHub.PauseGame, Message(new PauseGameRequest(Game.State.GameId, paused)), Ct);

    /// <summary>
    /// The game master shows the question, then both its choices: the last one starts the countdown of its answers.
    /// </summary>
    private async Task PresentAsync(HubConnection gameMaster)
    {
        await SendAsync(gameMaster, new QuizShowQuestion(RoundId, 1));
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, 1, QuizChoiceLetter.A));
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, 1, QuizChoiceLetter.B));
    }

    private static Task SendAsync(HubConnection gameMaster, GameMasterRoundIntent intent) =>
        gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message(intent), Ct);

    private Task AnswerAsync(HubConnection player, QuizChoiceLetter choice) =>
        _playerIntents.SendAsync(player, new QuizSubmitAnswer(RoundId, 1, choice));

    private static JsonElement Message<T>(T message) => JsonSerializer.SerializeToElement(message, ContractJsonOptions.Default);

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
