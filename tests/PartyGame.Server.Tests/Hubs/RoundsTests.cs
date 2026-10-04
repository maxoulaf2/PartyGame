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
using PartyGame.Tests.Shared.Leaks;

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
            .UseScratchDirectory(_logs.Path)
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
        // The TV screen shows nothing of the question until the game master reads it out.
        var onDisplay = Assert.IsType<QuizDisplayView>(toDisplay.Display[^1].RoundView);
        Assert.Equal((1, null), (onDisplay.QuestionNumber, onDisplay.Text));
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
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, new QuizSubmitAnswer(first, 1, QuizChoiceLetter.A));
        var played = (TestQuizRound)Game.State.CurrentRound!.State;
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizSkipQuestion(first, 1)), Ct);
        var betweenRounds = Game.State.Phase;
        await gameMaster.InvokeAsync(GameHub.NextRound, Message(new NextRoundRequest(first)), Ct);
        var second = Game.State.CurrentRound!;
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizSkipQuestion(second.Id, 1)), Ct);

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
    public async Task RoundFinished_BeforeTheLast_SendsTheRankingToEachInterface()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await JoinAsync(max, "Max");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var first = Game.State.CurrentRound!.Id;
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);

        // When
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizSkipQuestion(first, 1)), Ct);

        // Then: nobody scored, so both share the first rank, in alphabetical order
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        var ids = Game.State.Players.ToDictionary(p => p.Nickname, p => p.Id);
        RankedPlayer[] expected =
        [
            new(ids["Max"], "Max", IsConnected: true, Rank: 1, IsTied: true, Score: 0),
            new(ids["Zoé"], "Zoé", IsConnected: true, Rank: 1, IsTied: true, Score: 0),
        ];
        Assert.Equal(Phase.BetweenRounds, toDisplay.Display[^1].Phase);
        Assert.Equal(expected, toDisplay.Display[^1].Ranking);
        Assert.Equal(expected, toGameMaster.GameMaster[^1].Ranking);
        Assert.Equal("Finale", toGameMaster.GameMaster[^1].NextRoundTitle);
        Assert.Equal((new PlayerStanding(1, IsTied: true, RankedCount: 2), 2), (toZoe.Player[^1].Standing, toZoe.Player[^1].PlayerCount));
        Assert.Contains("\"standing\":{\"rank\":1,\"isTied\":true,\"rankedCount\":2}", toZoe.Json[^1], StringComparison.Ordinal);
        Assert.DoesNotContain("Finale", toDisplay.Json[^1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task LastRoundFinished_SendsTheFinalRankingToEachInterface()
    {
        // Given: Zoé scores in the first round, then the game master plays the game to its end
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await JoinAsync(max, "Max");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var first = Game.State.CurrentRound!.Id;
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, new QuizSubmitAnswer(first, 1, QuizChoiceLetter.A));
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizSkipQuestion(first, 1)), Ct);
        await gameMaster.InvokeAsync(GameHub.NextRound, Message(new NextRoundRequest(first)), Ct);
        var last = Game.State.CurrentRound!.Id;
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        using var toMax = new ReceivedSnapshots(max);

        // When
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizSkipQuestion(last, 1)), Ct);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe), FlushAsync(max));
        var ids = Game.State.Players.ToDictionary(p => p.Nickname, p => p.Id);
        RankedPlayer[] expected =
        [
            new(ids["Zoé"], "Zoé", IsConnected: true, Rank: 1, IsTied: false, Score: TestQuizMode.PointsPerIntent),
            new(ids["Max"], "Max", IsConnected: true, Rank: 2, IsTied: false, Score: 0),
        ];
        Assert.Equal(Phase.Finished, toDisplay.Display[^1].Phase);
        Assert.Equal(expected, toDisplay.Display[^1].Ranking);
        Assert.Equal(expected, toGameMaster.GameMaster[^1].Ranking);
        Assert.Equal(new PlayerStanding(1, IsTied: false, RankedCount: 2), toZoe.Player[^1].Standing);
        Assert.Equal(new PlayerStanding(2, IsTied: false, RankedCount: 2), toMax.Player[^1].Standing);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Json, new Secret("Max", Audience.OtherPlayersThan("Max")));
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Max"), toMax.Json, new Secret("Zoé", Audience.OtherPlayersThan("Zoé")));
    }

    [Fact]
    public async Task JoinGame_OnceFinished_ShowsTheEndWithoutRankingTheLateArrival()
    {
        // Given: a game played to its end by Zoé
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var lea = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var first = Game.State.CurrentRound!.Id;
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizSkipQuestion(first, 1)), Ct);
        await gameMaster.InvokeAsync(GameHub.NextRound, Message(new NextRoundRequest(first)), Ct);
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizSkipQuestion(Game.State.CurrentRound!.Id, 1)), Ct);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toLea = new ReceivedSnapshots(lea);

        // When: registration stays open
        await JoinAsync(lea, "Léa");

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(lea));
        Assert.Equal((Phase.Finished, null), (toLea.Player[^1].Phase, toLea.Player[^1].Standing));
        Assert.Equal(["Zoé"], toDisplay.Display[^1].Ranking.Select(p => p.Nickname));
        Assert.Contains(toDisplay.Display[^1].Players, p => p.Nickname == "Léa");
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
        await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizSkipQuestion(first, 1)), Ct);
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
        await PlayerIntents.SendAsync(gameMaster, clientSeq: 1, new QuizSubmitAnswer(state.CurrentRound!.Id, 1, QuizChoiceLetter.A));

        // Then
        Assert.Same(state, Game.State);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Level == "Warning" && e.Template.Contains("identified no player", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{"clientSeq":1,"intent":{"type":"buzz","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff"}}""")]
    [InlineData("""{"clientSeq":1,"intent":{"roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff"}}""")]
    [InlineData("""{"clientSeq":1,"intent":{"type":"quiz"}}""")]
    [InlineData("""{"clientSeq":1,"intent":{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff"}}""")]
    [InlineData("""{"clientSeq":1,"intent":{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","questionNumber":1,"choice":"E"}}""")]
    [InlineData("""{"clientSeq":1,"intent":{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","questionNumber":1,"choice":1}}""")]
    [InlineData("""{"clientSeq":1,"intent":null}""")]
    [InlineData("""{"clientSeq":1}""")]
    [InlineData("""{"intent":{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","questionNumber":1,"choice":"A"}}""")]
    [InlineData("""{"clientSeq":"1","intent":{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","questionNumber":1,"choice":"A"}}""")]
    [InlineData("""{"clientSeq":0,"intent":{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","questionNumber":1,"choice":"A"}}""")]
    [InlineData("""{"type":"quiz.submitAnswer","roundId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","questionNumber":1,"choice":"A"}""")]
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

    [Fact]
    public async Task SkipRound_InProgress_EndsTheRoundOnEveryInterfaceAndKeepsThePoints()
    {
        // Given: Zoé scored in the first round
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var first = Game.State.CurrentRound!.Id;
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, new QuizSubmitAnswer(first, 1, QuizChoiceLetter.A));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);

        // When
        await gameMaster.InvokeAsync(GameHub.SkipRound, Message(new SkipRoundRequest(first)), Ct);

        // Then: the usual end of round, and only the console knows it was skipped
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        Assert.Equal((GamePhase.BetweenRounds, true), (Game.State.Phase, Game.State.CurrentRound!.IsSkipped));
        Assert.Equal((Phase.BetweenRounds, first), (toDisplay.Display[^1].Phase, toDisplay.Display[^1].Round!.RoundId));
        Assert.Equal(TestQuizMode.PointsPerIntent, Assert.Single(toDisplay.Display[^1].Ranking).Score);
        Assert.Equal((Phase.BetweenRounds, TestQuizMode.PointsPerIntent), (toZoe.Player[^1].Phase, toZoe.Player[^1].Score));
        Assert.True(toGameMaster.GameMaster[^1].RoundSkipped);
        var skipped = new Secret(nameof(GameMasterSnapshot.RoundSkipped), Audience.AllButGameMaster);
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, skipped);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Json, skipped);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Level == "Information" && e.Template.Contains("skipped by the game master", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SkipRound_TwiceAtOnce_SkipsASingleRound()
    {
        // Given: two game master consoles during the first round
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var first = Game.State.CurrentRound!.Id;
        var version = Game.State.Version;

        // When
        await Task.WhenAll(
            gameMaster.InvokeAsync(GameHub.SkipRound, Message(new SkipRoundRequest(first)), Ct),
            secondGameMaster.InvokeAsync(GameHub.SkipRound, Message(new SkipRoundRequest(first)), Ct));

        // Then
        Assert.Equal((GamePhase.BetweenRounds, 0), (Game.State.Phase, Game.State.CurrentRound!.Index));
        Assert.Equal(version + 1, Game.State.Version);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"roundId":"pas-un-guid"}""")]
    [InlineData("null")]
    public async Task SkipRound_Malformed_IsIgnoredWithAWarning(string json)
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var state = Game.State;

        // When
        await gameMaster.InvokeAsync(GameHub.SkipRound, JsonDocument.Parse(json).RootElement, Ct);

        // Then
        Assert.Same(state, Game.State);
        Assert.Contains(
            LoggedEvent.ReadAll(_logs),
            e => e.Level == "Warning" && e.Template.StartsWith("Malformed", StringComparison.Ordinal)
                && e.Line.Contains(GameHub.SkipRound, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(GameHub.SendGameMasterRoundIntent)]
    [InlineData(GameHub.NextRound)]
    [InlineData(GameHub.SkipRound)]
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
            await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizSkipQuestion(roundId, 1)), Ct);
        }

        var state = Game.State;
        var message = method switch
        {
            GameHub.NextRound => Message(new NextRoundRequest(roundId)),
            GameHub.SkipRound => Message(new SkipRoundRequest(roundId)),
            _ => Message<GameMasterRoundIntent>(new QuizSkipQuestion(roundId, 1)),
        };

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
