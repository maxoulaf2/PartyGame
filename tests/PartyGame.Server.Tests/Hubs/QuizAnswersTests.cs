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
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// The answers of a quiz question through the hub, up to their reveal, with the real quiz mode and a clock the test drives.
/// </summary>
public sealed class QuizAnswersTests : IAsyncDisposable
{
    private const string Code = "482913";

    private static readonly DateTimeOffset _start = new(2026, 10, 3, 21, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly FakeTimeProvider _time = new(_start);
    private readonly PlayerIntents _playerIntents = new();
    private readonly WebApplicationFactory<Program> _factory;

    public QuizAnswersTests()
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
    public async Task SubmitAnswer_ThreePlayers_CountedOnTheDisplayAndListedOnTheConsoleOnlyThenLocked()
    {
        // Given: the answers of the question are open
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        await using var max = await JoinAsync("Max");
        await using var lea = await JoinAsync("Léa");
        Assert.Null((await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct)).Refusal);
        // What the previous intents sent is received first: only what follows is recorded.
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe), FlushAsync(max));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        using var toMax = new ReceivedSnapshots(max);
        await PresentAsync(gameMaster, 1);

        // When
        await AnswerAsync(zoe, QuizChoiceLetter.B);
        await AnswerAsync(max, QuizChoiceLetter.A);
        await AnswerAsync(lea, QuizChoiceLetter.B);

        // Then: the last answer locks them, without waiting for the end of the countdown
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe), FlushAsync(max));
        var closeAt = _start.AddSeconds(QuizRoundDescriptor.DefaultAnswerSeconds).ToUnixTimeMilliseconds();
        var beforeLast = Assert.IsType<QuizDisplayView>(toDisplay.Display[^2].RoundView);
        Assert.Equal((QuizQuestionPhase.Answering, closeAt, 2, 3), (beforeLast.Phase, beforeLast.AnswersCloseAt, beforeLast.AnsweredCount, beforeLast.ParticipantCount));
        var onDisplay = Assert.IsType<QuizDisplayView>(toDisplay.Display[^1].RoundView);
        Assert.Equal((QuizQuestionPhase.Locked, null, 3, 3), (onDisplay.Phase, onDisplay.AnswersCloseAt, onDisplay.AnsweredCount, onDisplay.ParticipantCount));
        var onConsole = Assert.IsType<QuizGameMasterView>(toGameMaster.GameMaster[^1].RoundView);
        Assert.Equal(
            [("Zoé", QuizChoiceLetter.B), ("Max", QuizChoiceLetter.A), ("Léa", QuizChoiceLetter.B)],
            onConsole.Answers.Select(answer => (answer.Nickname, answer.Choice!.Value)));
        Assert.Equal([1, 2], onConsole.Choices.Select(choice => choice.AnswerCount));
        Assert.Equal(QuizChoiceLetter.B, Assert.IsType<QuizPlayerView>(toZoe.Player[^1].RoundView).Answer);
        Assert.Equal(QuizChoiceLetter.A, Assert.IsType<QuizPlayerView>(toMax.Player[^1].RoundView).Answer);

        // A phone learns nothing of the other players, nor any text it could tell the correct answer from.
        foreach (var (viewer, json) in new[] { (Viewer.PhoneOf("Zoé"), toZoe.Json), (Viewer.PhoneOf("Max"), toMax.Json) })
        {
            LeakAssert.NoSecretReceived(viewer, json, new Secret("Léa", Audience.Everyone), new Secret("Oui", Audience.Everyone));
        }
    }

    [Fact]
    public async Task SubmitAnswer_Twice_KeepsTheFirstChoice()
    {
        // Given: Max has not answered, so that the answers stay open
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await using var max = await JoinAsync("Max");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await PresentAsync(gameMaster, 1);
        await AnswerAsync(zoe, QuizChoiceLetter.B);
        var version = Game.State.Version;

        // When
        await AnswerAsync(zoe, QuizChoiceLetter.A);

        // Then
        Assert.Equal(version, Game.State.Version);
        var view = Assert.IsType<QuizGameMasterView>(_factory.Services.GetRequiredService<Engine.Projections.Snapshots>().ForGameMaster(Game.State).RoundView);
        Assert.Equal(QuizChoiceLetter.B, view.Answers[0].Choice);
    }

    [Fact]
    public async Task SubmitAnswer_SentAgainWithTheSameClientSeq_IsHandledOnce()
    {
        // Given: Zoé answered, and her phone lost the acknowledgment
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        var joined = await zoe.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await PresentAsync(gameMaster, 1);
        var answer = new QuizSubmitAnswer(RoundId, 1, QuizChoiceLetter.B);
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, answer);

        // When: identified again on a new connection, before the server noticed the first one dropped, it sends the
        // answer again with the same number, then a stray copy of it with another choice
        await using var reconnected = await HubClients.ConnectAsync(_factory);
        var resumed = await reconnected.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, new ResumeSessionRequest(joined.Token!), Ct);
        var version = Game.State.Version;
        await PlayerIntents.SendAsync(reconnected, clientSeq: 1, answer);
        await PlayerIntents.SendAsync(reconnected, clientSeq: 1, answer with { Choice = QuizChoiceLetter.A });

        // Then: neither changes the game
        Assert.Null(resumed.Refusal);
        Assert.Equal(version, Game.State.Version);
        var view = Assert.IsType<QuizGameMasterView>(_factory.Services.GetRequiredService<Engine.Projections.Snapshots>().ForGameMaster(Game.State).RoundView);
        Assert.Equal(QuizChoiceLetter.B, Assert.Single(view.Answers).Choice);
        Assert.Equal(1, Assert.Single(Game.State.Players).LastClientSeq);
    }

    [Fact]
    public async Task RecoverSession_AfterAnAnswer_GivesTheLastClientSeqSoThatTheNewPhoneGoesOnNumbering()
    {
        // Given: Zoé answered from a phone she then lost
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        var joined = await zoe.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await PresentAsync(gameMaster, 1);
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, new QuizSubmitAnswer(RoundId, 1, QuizChoiceLetter.B));
        var code = Game.State.ReconnectionCodes[joined.PlayerId!.Value];

        // When
        await using var newPhone = await HubClients.ConnectAsync(_factory);
        var recovered = await newPhone.InvokeAsync<RecoverSessionResult>(GameHub.RecoverSession, new RecoverSessionRequest(code), Ct);

        // Then
        Assert.Equal(new RecoverSessionResult(Refusal: null, joined.Token, LastClientSeq: 1), recovered);
    }

    [Fact]
    public async Task AnswersTimer_Elapses_LocksTheAnswersOnEveryInterface()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await PresentAsync(gameMaster, 1);
        // What the previous intents sent is received first: only what follows is recorded.
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toZoe = new ReceivedSnapshots(zoe);
        var version = Game.State.Version;

        // When: the countdown runs out without any answer
        _time.Advance(TimeSpan.FromSeconds(QuizRoundDescriptor.DefaultAnswerSeconds));
        await WaitUntilAsync(() => Game.State.Version > version);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe));
        var onDisplay = Assert.IsType<QuizDisplayView>(toDisplay.Display[^1].RoundView);
        Assert.Equal((QuizQuestionPhase.Locked, null, 0, 1), (onDisplay.Phase, onDisplay.AnswersCloseAt, onDisplay.AnsweredCount, onDisplay.ParticipantCount));
        var onPhone = Assert.IsType<QuizPlayerView>(toZoe.Player[^1].RoundView);
        Assert.Equal((QuizQuestionPhase.Locked, null), (onPhone.Phase, onPhone.Answer));

        // An answer that arrives once the answers are locked does not count.
        await AnswerAsync(zoe, QuizChoiceLetter.A);
        Assert.Equal(version + 1, Game.State.Version);
    }

    [Fact]
    public async Task SubmitAnswer_LastParticipant_LocksTheAnswersAndCancelsTheTimer()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await PresentAsync(gameMaster, 1);

        // When: the only participant answers, then the countdown would have run out
        await AnswerAsync(zoe, QuizChoiceLetter.A);
        var locked = Game.State;
        _time.Advance(TimeSpan.FromSeconds(QuizRoundDescriptor.DefaultAnswerSeconds));
        await FlushAsync(gameMaster);

        // Then: the cancelled timer changes nothing
        Assert.Same(locked, Game.State);
        var view = Assert.IsType<QuizGameMasterView>(_factory.Services.GetRequiredService<Engine.Projections.Snapshots>().ForGameMaster(locked).RoundView);
        Assert.Equal((QuizQuestionPhase.Locked, QuizChoiceLetter.A), (view.Phase, Assert.Single(view.Answers).Choice));
    }

    [Fact]
    public async Task RevealAnswer_OnceLocked_ShowsWhoChoseWhatOnTheDisplayAndEachVerdictAndScoreOnThePhones()
    {
        // Given: Zoé is right, Max is wrong, Léa did not answer, and the answers are locked
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        await using var max = await JoinAsync("Max");
        await using var lea = await JoinAsync("Léa");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await PresentAsync(gameMaster, 1);
        await AnswerAsync(zoe, QuizChoiceLetter.A);
        await AnswerAsync(max, QuizChoiceLetter.B);
        var version = Game.State.Version;
        _time.Advance(TimeSpan.FromSeconds(QuizRoundDescriptor.DefaultAnswerSeconds));
        await WaitUntilAsync(() => Game.State.Version > version);
        // What the previous intents sent is received first: only what follows is recorded.
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe), FlushAsync(max), FlushAsync(lea));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toZoe = new ReceivedSnapshots(zoe);
        using var toMax = new ReceivedSnapshots(max);
        using var toLea = new ReceivedSnapshots(lea);

        // When: the game master reveals, then sends it again, as a second console would
        await SendAsync(gameMaster, new QuizRevealAnswer(RoundId, 1));
        var revealed = Game.State;
        await SendAsync(gameMaster, new QuizRevealAnswer(RoundId, 1));

        // Then
        Assert.Same(revealed, Game.State);
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe), FlushAsync(max), FlushAsync(lea));
        var onDisplay = Assert.IsType<QuizDisplayView>(Assert.Single(toDisplay.Display).RoundView);
        Assert.Equal(QuizQuestionPhase.Revealed, onDisplay.Phase);
        Assert.Equal(QuizChoiceLetter.A, onDisplay.Reveal!.CorrectChoice);
        Assert.Equal(
            [("Zoé", QuizChoiceLetter.A), ("Max", QuizChoiceLetter.B), ("Léa", null)],
            onDisplay.Reveal.Answers.Select(answer => (answer.Nickname, answer.Choice)));
        var onPhones = new[] { toZoe, toMax, toLea }.Select(to => Assert.Single(to.Player)).ToArray();
        var views = onPhones.Select(snapshot => Assert.IsType<QuizPlayerView>(snapshot.RoundView)).ToArray();
        Assert.Equal([QuizVerdict.Correct, QuizVerdict.Wrong, QuizVerdict.NoAnswer], views.Select(view => view.Verdict!.Value));

        // The points of the question, awarded once although the reveal was sent twice.
        Assert.Equal([1000, 0, 0], views.Select(view => view.Points!.Value));
        Assert.Equal([1000, 0, 0], onPhones.Select(snapshot => snapshot.Score));

        // A phone learns its own verdict only, never who the others are nor what they chose.
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Léa"), toLea.Json, new Secret("Zoé", Audience.Everyone), new Secret("Max", Audience.Everyone));
    }

    [Fact]
    public async Task SubmitAnswer_WhileTheChoicesShow_LastChoiceLocksTheAnswersWithoutCountdown()
    {
        // Given: Zoé, the only participant, answers as soon as the first choice shows
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await SendAsync(gameMaster, new QuizShowQuestion(RoundId, 1));
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, 1, QuizChoiceLetter.A));
        // What the previous intents sent is received first: only what follows is recorded.
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toZoe = new ReceivedSnapshots(zoe);
        await AnswerAsync(zoe, QuizChoiceLetter.A);

        // When: the game master shows the last choice, then the countdown would have run out
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, 1, QuizChoiceLetter.B));
        var locked = Game.State;
        _time.Advance(TimeSpan.FromSeconds(QuizRoundDescriptor.DefaultAnswerSeconds));
        await FlushAsync(gameMaster);

        // Then: the answers locked at once, without any countdown, and no timer changes them since
        Assert.Same(locked, Game.State);
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe));
        var views = toDisplay.Display.Select(snapshot => Assert.IsType<QuizDisplayView>(snapshot.RoundView)).ToList();
        Assert.Equal(
            [(QuizQuestionPhase.Presentation, 1), (QuizQuestionPhase.Locked, 2)],
            views.Select(view => (view.Phase, view.Choices.Length)));
        Assert.All(views, view => Assert.Null(view.AnswersCloseAt));
        Assert.Equal((1, 1), (views[^1].AnsweredCount, views[^1].ParticipantCount));
        var onPhone = Assert.IsType<QuizPlayerView>(toZoe.Player[0].RoundView);
        Assert.Equal((QuizQuestionPhase.Presentation, 1, QuizChoiceLetter.A), (onPhone.Phase, onPhone.ShownChoiceCount, onPhone.Answer));
    }

    [Fact]
    public async Task SubmitAnswer_ChoiceNotShownYet_IsIgnored()
    {
        // Given: the first choice shows, the second one not yet
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await SendAsync(gameMaster, new QuizShowQuestion(RoundId, 1));
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, 1, QuizChoiceLetter.A));
        var state = Game.State;

        // When
        await AnswerAsync(zoe, QuizChoiceLetter.B);

        // Then
        Assert.Same(state, Game.State);
    }

    [Fact]
    public async Task ShowChoice_FromAPlayer_IsIgnored()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        await SendAsync(gameMaster, new QuizShowQuestion(RoundId, 1));
        var state = Game.State;

        // When: a player sends what only the game master may send
        await zoe.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizShowChoice(RoundId, 1, QuizChoiceLetter.A)), Ct);

        // Then
        Assert.Same(state, Game.State);
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

    /// <summary>
    /// The game master shows the question, then both its choices: the last one starts the countdown of its answers.
    /// </summary>
    private async Task PresentAsync(HubConnection gameMaster, int questionNumber)
    {
        await SendAsync(gameMaster, new QuizShowQuestion(RoundId, questionNumber));
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, questionNumber, QuizChoiceLetter.A));
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, questionNumber, QuizChoiceLetter.B));
    }

    private Task AnswerAsync(HubConnection player, QuizChoiceLetter choice) =>
        _playerIntents.SendAsync(player, new QuizSubmitAnswer(RoundId, 1, choice));

    /// <summary>
    /// A message as the client sends it: serialized as its declared type, so that a round intent carries its <c>type</c>.
    /// </summary>
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
