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
/// The questions of a quiz round through the hub, one after the other, some of them skipped, up to the end of the round.
/// </summary>
public sealed class QuizMoveOnTests : IAsyncDisposable
{
    private const string Code = "482913";

    private static readonly DateTimeOffset _start = new(2026, 10, 3, 21, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly FakeTimeProvider _time = new(_start);
    private readonly PlayerIntents _playerIntents = new();
    private readonly WebApplicationFactory<Program> _factory;

    public QuizMoveOnTests()
    {
        // The only pack of the directory, chosen at once: a single round of three questions.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.LongQuiz("Soirée test", 3));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("LogFiles:Directory", _logs.Path)
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
    public async Task NextQuestion_SentTwice_PresentsTheNextQuestionOnceOnEveryInterface()
    {
        // Given: the first question is revealed
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await SendAsync(gameMaster, new QuizOpenAnswers(RoundId, 1));
        await AnswerAsync(zoe, 1, QuizChoiceLetter.A);
        await SendAsync(gameMaster, new QuizLockAnswers(RoundId, 1));
        await SendAsync(gameMaster, new QuizRevealAnswer(RoundId, 1));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toZoe = new ReceivedSnapshots(zoe);

        // When: the game master moves on, then sends it again, as a second console would
        await SendAsync(gameMaster, new QuizNextQuestion(RoundId, 1));
        var next = Game.State;
        await SendAsync(gameMaster, new QuizNextQuestion(RoundId, 1));

        // Then
        Assert.Same(next, Game.State);
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe));
        var onDisplay = Assert.IsType<QuizDisplayView>(Assert.Single(toDisplay.Display).RoundView);
        Assert.Equal((2, 3, QuizQuestionPhase.Presentation, "Question 2 ?"), (onDisplay.QuestionNumber, onDisplay.QuestionCount, onDisplay.Phase, onDisplay.Text));
        Assert.Null(onDisplay.Reveal);
        var onPhone = Assert.IsType<QuizPlayerView>(Assert.Single(toZoe.Player).RoundView);
        Assert.Equal((2, QuizQuestionPhase.Presentation, null, null), (onPhone.QuestionNumber, onPhone.Phase, onPhone.Answer, onPhone.Verdict));
    }

    [Fact]
    public async Task SkipQuestion_WhileTheAnswersAreOpen_MovesOnAndStopsTheirCountdown()
    {
        // Given: the answers of the first question are open, and Zoé answered
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await SendAsync(gameMaster, new QuizOpenAnswers(RoundId, 1));
        await AnswerAsync(zoe, 1, QuizChoiceLetter.A);
        using var toDisplay = new ReceivedSnapshots(display);

        // When: the game master skips it, sends it again, and the countdown would have run out meanwhile
        await SendAsync(gameMaster, new QuizSkipQuestion(RoundId, 1));
        var skipped = Game.State;
        await SendAsync(gameMaster, new QuizSkipQuestion(RoundId, 1));
        _time.Advance(TimeSpan.FromSeconds(QuizRoundDescriptor.DefaultAnswerSeconds));
        await FlushAsync(gameMaster);

        // Then: the second question is presented, and neither the second skip nor the cancelled timer changes anything
        Assert.Same(skipped, Game.State);
        await FlushAsync(display);
        var onDisplay = Assert.IsType<QuizDisplayView>(Assert.Single(toDisplay.Display).RoundView);
        Assert.Equal((2, 3, QuizQuestionPhase.Presentation, 0), (onDisplay.QuestionNumber, onDisplay.QuestionCount, onDisplay.Phase, onDisplay.AnsweredCount));

        // The answer to the skipped question, sent again late, does not count for the next one.
        await SendAsync(gameMaster, new QuizOpenAnswers(RoundId, 2));
        var opened = Game.State;
        await AnswerAsync(zoe, 1, QuizChoiceLetter.B);
        Assert.Same(opened, Game.State);
    }

    [Fact]
    public async Task NextQuestion_AfterTheLastQuestion_EndsTheRound()
    {
        // Given: the first two questions are skipped, and the last one revealed
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await SendAsync(gameMaster, new QuizSkipQuestion(RoundId, 1));
        await SendAsync(gameMaster, new QuizSkipQuestion(RoundId, 2));
        await SendAsync(gameMaster, new QuizOpenAnswers(RoundId, 3));
        await SendAsync(gameMaster, new QuizLockAnswers(RoundId, 3));
        await SendAsync(gameMaster, new QuizRevealAnswer(RoundId, 3));
        using var toDisplay = new ReceivedSnapshots(display);

        // When
        await SendAsync(gameMaster, new QuizNextQuestion(RoundId, 3));
        var finished = Game.State;
        await SendAsync(gameMaster, new QuizNextQuestion(RoundId, 3));

        // Then: the round was the only one of the pack
        Assert.Same(finished, Game.State);
        await FlushAsync(display);
        var snapshot = Assert.Single(toDisplay.Display);
        Assert.Equal(Phase.Finished, snapshot.Phase);
        Assert.Null(snapshot.RoundView);
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

    private static Task SendAsync(HubConnection gameMaster, GameMasterRoundIntent intent) =>
        gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message(intent), Ct);

    private Task AnswerAsync(HubConnection player, int questionNumber, QuizChoiceLetter choice) =>
        _playerIntents.SendAsync(player, new QuizSubmitAnswer(RoundId, questionNumber, choice));

    /// <summary>
    /// A message as the client sends it: serialized as its declared type, so that a round intent carries its <c>type</c>.
    /// </summary>
    private static JsonElement Message<T>(T message) => JsonSerializer.SerializeToElement(message, ContractJsonOptions.Default);

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
