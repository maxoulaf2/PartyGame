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
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class RoundsTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public RoundsTests()
    {
        // The only pack of the directory, chosen at once: its rounds are played by a test mode.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Échauffement", "Finale"));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("LogFiles:Directory", _logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path)
            .ConfigureTestServices(services =>
            {
                // The test mode replaces the quiz mode, whose rounds cannot be played to their end yet.
                services.RemoveAll<IGameMode>();
                services.AddSingleton<IGameMode, TestQuizMode>();
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
    public async Task StartGame_WithRounds_ShowsTheFirstRoundAndTheViewOfItsModeOnEveryInterface()
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

        // When
        Assert.Null((await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct)).Refusal);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        var round = new RoundInfo(Game.State.CurrentRound!.Id, Number: 1, Count: 2, "Échauffement");
        Assert.Equal((Phase.Round, round), (toDisplay.Display[^1].Phase, toDisplay.Display[^1].Round));
        Assert.Equal((Phase.Round, round), (toGameMaster.GameMaster[^1].Phase, toGameMaster.GameMaster[^1].Round));
        Assert.Equal((Phase.Round, round), (toZoe.Player[^1].Phase, toZoe.Player[^1].Round));
        Assert.Equal("Question ?", Assert.IsType<QuizDisplayView>(toDisplay.Display[^1].RoundView).Text);
        Assert.Equal("Question ?", Assert.IsType<QuizGameMasterView>(toGameMaster.GameMaster[^1].RoundView).Text);
        Assert.Equal(1, Assert.IsType<QuizPlayerView>(toZoe.Player[^1].RoundView).QuestionNumber);
        Assert.Contains("\"roundView\":{\"type\":\"quiz\",", toZoe.Json[^1], StringComparison.Ordinal);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Round {RoundNumber} of {RoundCount} started", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rounds_PlayedToTheEnd_GoBetweenRoundsThenFinishTheGame()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var first = Game.State.CurrentRound!.Id;

        // When: a player acts, the game master ends the round, asks for the next one and ends it as well
        await zoe.InvokeAsync(GameHub.SendRoundIntent, Message<PlayerRoundIntent>(new QuizSubmitAnswer(first, 1, QuizChoiceLetter.A)), Ct);
        var played = (TestQuizRound)Game.State.CurrentRound!.State;
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizLockAnswers(first, 1)), Ct);
        var betweenRounds = Game.State.Phase;
        await gameMaster.InvokeAsync(GameHub.NextRound, Message(new NextRoundRequest(first)), Ct);
        var second = Game.State.CurrentRound!;
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizLockAnswers(second.Id, 1)), Ct);

        // Then
        Assert.Equal(1, played.PlayerIntents);
        Assert.Equal(GamePhase.BetweenRounds, betweenRounds);
        Assert.Equal(1, second.Index);
        Assert.NotEqual(first, second.Id);
        Assert.Equal(GamePhase.Finished, Game.State.Phase);
        var templates = LoggedEvent.ReadAll(_logs).Select(e => e.Template).ToList();
        Assert.Equal(2, templates.Count(t => t.StartsWith("Round {RoundNumber} of {RoundCount} finished", StringComparison.Ordinal)));
        Assert.Contains("Game finished", templates);
    }

    [Fact]
    public async Task NextRound_TwiceAtOnce_StartsTheNextRoundOnce()
    {
        // Given: two game master consoles between the rounds
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var first = Game.State.CurrentRound!.Id;
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizLockAnswers(first, 1)), Ct);
        var version = Game.State.Version;

        // When
        await Task.WhenAll(
            gameMaster.InvokeAsync(GameHub.NextRound, Message(new NextRoundRequest(first)), Ct),
            secondGameMaster.InvokeAsync(GameHub.NextRound, Message(new NextRoundRequest(first)), Ct));

        // Then
        Assert.Equal((GamePhase.Round, 1), (Game.State.Phase, Game.State.CurrentRound!.Index));
        Assert.Equal(version + 1, Game.State.Version);
    }

    [Fact]
    public async Task SendRoundIntent_FromAConnectionWithoutPlayer_IsIgnored()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var state = Game.State;

        // When: the console, which identified no player, tries to act for one
        await gameMaster.InvokeAsync(GameHub.SendRoundIntent, Message<PlayerRoundIntent>(new QuizSubmitAnswer(state.CurrentRound!.Id, 1, QuizChoiceLetter.A)), Ct);

        // Then
        Assert.Same(state, Game.State);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Level == "Warning" && e.Template.Contains("identified no player", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{"type":"buzz","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff"}""")]
    [InlineData("""{"roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff"}""")]
    [InlineData("""{"type":"quiz"}""")]
    [InlineData("""{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff"}""")]
    [InlineData("""{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","questionNumber":1,"choice":"E"}""")]
    [InlineData("""{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","questionNumber":1,"choice":1}""")]
    public async Task SendRoundIntent_Malformed_IsIgnoredWithAWarning(string json)
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var state = Game.State;

        // When
        await zoe.InvokeAsync(GameHub.SendRoundIntent, JsonDocument.Parse(json).RootElement, Ct);

        // Then
        Assert.Same(state, Game.State);
        Assert.Contains(
            LoggedEvent.ReadAll(_logs),
            e => e.Level == "Warning" && e.Template.StartsWith("Malformed", StringComparison.Ordinal)
                && e.Line.Contains(GameHub.SendRoundIntent, StringComparison.Ordinal));
        Assert.DoesNotContain(LoggedEvent.ReadAll(_logs), e => e.Level == "Error");
    }

    [Theory]
    [InlineData(GameHub.SendGameMasterRoundIntent)]
    [InlineData(GameHub.NextRound)]
    public async Task GameMasterRoundIntent_NotAuthenticatedAsGameMaster_IsIgnored(string method)
    {
        // Given: the round finished, so that the next round could start
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var roundId = Game.State.CurrentRound!.Id;
        if (method == GameHub.NextRound)
        {
            await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizLockAnswers(roundId, 1)), Ct);
        }

        var state = Game.State;
        var message = method == GameHub.NextRound
            ? Message(new NextRoundRequest(roundId))
            : Message<GameMasterRoundIntent>(new QuizLockAnswers(roundId, 1));

        // When: a player sends what only the game master may send
        await zoe.InvokeAsync(method, message, Ct);

        // Then
        Assert.Same(state, Game.State);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("not authenticated as game master", StringComparison.Ordinal));
    }

    /// <summary>
    /// A message as the client sends it: serialized as its declared type, so that a round intent carries its <c>type</c>.
    /// </summary>
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
