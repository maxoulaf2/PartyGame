using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Hubs;
using PartyGame.Server.Tests.Packs;

namespace PartyGame.Server.Tests.Persistence;

/// <summary>
/// A game resumed by the game master after the server stopped in the middle of a question: the phones, the TV screen and
/// the console find it where it stopped, its countdown going on with the time it had left.
/// </summary>
public sealed class TransparentResumeTests : IAsyncDisposable
{
    private const string PreviousCode = "135790";
    private const string Code = "246802";
    private const string Image = "images/drapeau.png";

    private static readonly DateTimeOffset _start = new(2026, 10, 4, 21, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _data = new();
    private readonly TempDirectory _packs = new();

    // Shared by both runs, so that the time spent offline is the one the test chooses.
    private readonly FakeTimeProvider _time = new(_start);
    private WebApplicationFactory<Program>? _factory;

    public TransparentResumeTests()
    {
        // The only pack of the directory, chosen at once: two questions, "Oui" (A) being their correct answer.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.IllustratedQuiz("Soirée test", Image, Image));
        TestPacks.WriteMedia(_packs.Path, "soiree", Image, [1, 2, 3]);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory!.Services.GetRequiredService<GameLoop>();

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        _data.Dispose();
        _packs.Dispose();
    }

    [Fact]
    public async Task ResolveSavedGame_ResumeDuringTheCountdown_EveryScreenFindsTheQuestionWithTheTimeItHadLeft()
    {
        // Given: Zoé answered 5 seconds into the countdown, then the server stopped, and restarted 10 minutes later
        var (tokens, roundId) = await PlayFirstRunAsync();
        _time.Advance(TimeSpan.FromMinutes(10));
        Restart(mode: null);
        await using var display = await HubClients.ConnectAsync(_factory!);
        await AnnounceAsync(display, Role.Display, code: null);
        await using var gameMaster = await HubClients.ConnectAsync(_factory!);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);
        await FlushAsync(display, gameMaster);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When
        await gameMaster.InvokeAsync(GameHub.ResolveSavedGame, new ResolveSavedGameRequest(Game.State.PendingGame!.Game.GameId, Resume: true), Ct);
        await using var zoe = await HubClients.ConnectAsync(_factory!);
        using var toZoe = new ReceivedSnapshots(zoe);
        var resumed = await zoe.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, new ResumeSessionRequest(tokens[0]), Ct);

        // Then: 15 seconds left from the resumption, on every screen
        Assert.Null(resumed.Refusal);
        await FlushAsync(display, gameMaster, zoe);
        var closeAt = _time.GetUtcNow().AddSeconds(15).ToUnixTimeMilliseconds();
        var onDisplay = Assert.IsType<QuizDisplayView>(toDisplay.Display[^1].RoundView);
        Assert.Equal((QuizQuestionPhase.Answering, closeAt, 1), (onDisplay.Phase, onDisplay.AnswersCloseAt, onDisplay.AnsweredCount));
        var onConsole = Assert.IsType<QuizGameMasterView>(toGameMaster.GameMaster[^1].RoundView);
        Assert.Equal((QuizQuestionPhase.Answering, closeAt), (onConsole.Phase, onConsole.AnswersCloseAt));
        var onPhone = toZoe.Player[^1];
        var zoeView = Assert.IsType<QuizPlayerView>(onPhone.RoundView);
        Assert.Equal(("Zoé", 1, closeAt, QuizChoiceLetter.A), (onPhone.Nickname, zoeView.QuestionNumber, zoeView.AnswersCloseAt, zoeView.Answer));

        // The image of the question, at the URL the saved game drew for it.
        using var http = _factory!.CreateClient();
        var image = await http.GetAsync(new Uri(onDisplay.ImageUrl!, UriKind.Relative), Ct);
        Assert.Equal([1, 2, 3], await image.Content.ReadAsByteArrayAsync(Ct));

        // The answer Zoé sends again is ignored, the one Max's phone never got through is accepted.
        await using var max = await HubClients.ConnectAsync(_factory!);
        Assert.Null((await max.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, new ResumeSessionRequest(tokens[1]), Ct)).Refusal);
        var version = Game.State.Version;
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, new QuizSubmitAnswer(roundId, 1, QuizChoiceLetter.B));
        Assert.Equal(version, Game.State.Version);
        await PlayerIntents.SendAsync(max, clientSeq: 1, new QuizSubmitAnswer(roundId, 1, QuizChoiceLetter.B));
        var answers = Assert.IsType<QuizRound>(Game.State.CurrentRound!.State).Answers;
        Assert.Equal(
            [QuizChoiceLetter.A, QuizChoiceLetter.B],
            Game.State.Players.Take(2).Select(p => answers[p.Id].Choice));
    }

    [Fact]
    public async Task ResolveSavedGame_ResumeDuringTheCountdown_LocksTheAnswersAtTheNewDeadline()
    {
        // Given
        await PlayFirstRunAsync();
        _time.Advance(TimeSpan.FromMinutes(10));
        Restart(mode: null);
        await using var gameMaster = await HubClients.ConnectAsync(_factory!);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);
        await gameMaster.InvokeAsync(GameHub.ResolveSavedGame, new ResolveSavedGameRequest(Game.State.PendingGame!.Game.GameId, Resume: true), Ct);
        var version = Game.State.Version;

        // When: the 15 seconds left run out
        _time.Advance(TimeSpan.FromSeconds(14));
        await FlushAsync(gameMaster);
        Assert.Equal(version, Game.State.Version);
        _time.Advance(TimeSpan.FromSeconds(1));
        await WaitUntilAsync(() => Game.State.Version > version);

        // Then
        Assert.Equal(QuizPhase.Locked, Assert.IsType<QuizRound>(Game.State.CurrentRound!.State).Phase);
    }

    [Fact]
    public async Task ResolveSavedGame_RoundFailsToResume_ResumesTheGameAndTellsTheGameMaster()
    {
        // Given: the mode of the round throws when it resumes
        await PlayFirstRunAsync(new FaultyQuizMode());
        Restart(new FaultyQuizMode());
        await using var gameMaster = await HubClients.ConnectAsync(_factory!);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);

        // When
        await gameMaster.InvokeAsync(GameHub.ResolveSavedGame, new ResolveSavedGameRequest(Game.State.PendingGame!.Game.GameId, Resume: true), Ct);

        // Then: the game goes on where it stopped, and only the game master learns that something went wrong
        Assert.Equal(GamePhase.Round, Game.State.Phase);
        await FlushAsync(gameMaster);
        var incident = Assert.Single(toGameMaster.Incidents[^1].Incidents);
        Assert.Equal((IncidentCode.RoundHandlerFailed, 1), (incident.Code, incident.Round!.Number));
    }

    /// <summary>
    /// Plays a first run of the server: Zoé, Max and Léa join, the game master starts the game and, with the quiz mode,
    /// shows the first question with its choices, which starts the countdown, and Zoé answers right 5 seconds later. The
    /// server then stops, saving the game.
    /// </summary>
    /// <param name="mode">The mode that plays the rounds instead of the quiz mode, which saves the same state type.</param>
    private async Task<(string[] Tokens, RoundId RoundId)> PlayFirstRunAsync(IGameMode? mode = null)
    {
        var factory = CreateFactory(PreviousCode, mode);
        var tokens = new List<string>();
        var phones = new List<HubConnection>();
        foreach (var nickname in new[] { "Zoé", "Max", "Léa" })
        {
            var phone = await HubClients.ConnectAsync(factory);
            phones.Add(phone);
            tokens.Add((await phone.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct)).Token!);
        }

        await using var gameMaster = await HubClients.ConnectAsync(factory);
        await AnnounceAsync(gameMaster, Role.GameMaster, PreviousCode);
        Assert.Null((await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct)).Refusal);
        var roundId = factory.Services.GetRequiredService<GameLoop>().State.CurrentRound!.Id;
        if (mode is not null)
        {
            await DisposeAllAsync(factory, phones);
            return ([.. tokens], roundId);
        }

        await SendAsync(gameMaster, new QuizShowQuestion(roundId, 1));
        await SendAsync(gameMaster, new QuizShowChoice(roundId, 1, QuizChoiceLetter.A));
        await SendAsync(gameMaster, new QuizShowChoice(roundId, 1, QuizChoiceLetter.B));
        _time.Advance(TimeSpan.FromSeconds(5));
        await PlayerIntents.SendAsync(phones[0], clientSeq: 1, new QuizSubmitAnswer(roundId, 1, QuizChoiceLetter.A));

        await DisposeAllAsync(factory, phones);
        return ([.. tokens], roundId);
    }

    private static async Task DisposeAllAsync(WebApplicationFactory<Program> factory, List<HubConnection> phones)
    {
        foreach (var phone in phones)
        {
            await phone.DisposeAsync();
        }

        await factory.DisposeAsync();
    }

    private void Restart(IGameMode? mode) => _ = (_factory = CreateFactory(Code, mode)).Services;

    private WebApplicationFactory<Program> CreateFactory(string code, IGameMode? mode) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_data.Path)
            .UseSetting("GameMaster:Code", code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path)
            .ConfigureTestServices(services =>
            {
                services.AddSingleton<TimeProvider>(_time);
                if (mode is not null)
                {
                    services.RemoveAll<IGameMode>();
                    services.AddSingleton(mode);
                }
            }));

    private static async Task AnnounceAsync(HubConnection connection, Role role, string? code) =>
        Assert.Null((await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(role, code), Ct)).Refusal);

    private static Task SendAsync(HubConnection gameMaster, GameMasterRoundIntent intent) =>
        gameMaster.InvokeAsync(
            GameHub.SendGameMasterRoundIntent,
            System.Text.Json.JsonSerializer.SerializeToElement(intent, Contracts.Serialization.ContractJsonOptions.Default),
            Ct);

    private async Task FlushAsync(params HubConnection[] connections)
    {
        foreach (var connection in connections)
        {
            await HubClients.FlushAsync<GameHub>(_factory!, connection);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            await Task.Delay(20, timeout.Token);
        }
    }
}
