using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// The presentation of a quiz question through the hub: the TV screen shows the question, then its choices one by one,
/// as the game master reads them out, and receives nothing of them before.
/// </summary>
public sealed class QuizPresentationTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public QuizPresentationTests()
    {
        // The only pack of the directory, chosen at once: a single question, "Question ?", choices "Oui" (A) and "Non" (B).
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Manche"));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path));
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
    public async Task ShowQuestionAndChoices_OneByOne_ReachTheDisplayAsTheGameMasterGoes()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        using var toDisplay = new ReceivedSnapshots(display);
        Assert.Null((await gameMaster.StartGameAndFirstRoundAsync(Game, Ct)).Refusal);
        await FlushAsync(display);
        var presented = toDisplay.Json.Count;

        // When: the game master shows the question, then its first choice twice, as a second console would
        await SendAsync(gameMaster, new QuizShowQuestion(RoundId, 1));
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, 1, QuizChoiceLetter.A));
        var shown = Game.State;
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, 1, QuizChoiceLetter.A));

        // Then: one snapshot per step, the second choice still hidden
        Assert.Same(shown, Game.State);
        await FlushAsync(display);
        var views = toDisplay.Display.Skip(presented).Select(s => Assert.IsType<QuizDisplayView>(s.RoundView)).ToList();
        Assert.Equal(2, views.Count);
        Assert.Equal(("Question ?", 0), (views[0].Text, views[0].Choices.Length));
        Assert.Equal([new QuizChoiceView(QuizChoiceLetter.A, "Oui")], views[1].Choices);
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, new Secret("Non", Audience.Everyone));
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json.Take(presented), new Secret("Question ?", Audience.Everyone));
    }

    [Fact]
    public async Task ShowChoice_Last_StartsTheCountdownOnTheDisplay()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(display, Role.Display);
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        await SendAsync(gameMaster, new QuizShowQuestion(RoundId, 1));
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, 1, QuizChoiceLetter.A));
        await FlushAsync(display);
        using var toDisplay = new ReceivedSnapshots(display);

        // When
        await SendAsync(gameMaster, new QuizShowChoice(RoundId, 1, QuizChoiceLetter.B));

        // Then
        await FlushAsync(display);
        var view = Assert.IsType<QuizDisplayView>(Assert.Single(toDisplay.Display).RoundView);
        Assert.Equal(QuizQuestionPhase.Answering, view.Phase);
        Assert.NotNull(view.AnswersCloseAt);
        Assert.Equal(["Oui", "Non"], view.Choices.Select(c => c.Text));
    }

    [Fact]
    public async Task ShowQuestion_FromAPlayer_IsIgnored()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await JoinAsync("Zoé");
        await gameMaster.StartGameAndFirstRoundAsync(Game, Ct);
        var state = Game.State;

        // When: a player sends what only the game master may send
        await zoe.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message<GameMasterRoundIntent>(new QuizShowQuestion(RoundId, 1)), Ct);

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
    /// A message as the client sends it: serialized as its declared type, so that a round intent carries its <c>type</c>.
    /// </summary>
    private static JsonElement Message<T>(T message) => JsonSerializer.SerializeToElement(message, ContractJsonOptions.Default);

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
