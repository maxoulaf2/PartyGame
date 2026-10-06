using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Incidents;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Hubs;
using PartyGame.Server.Tests.Packs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Faults;

/// <summary>
/// A server configured to fail on purpose, as the E2E tests and the check of the exit criterion of phase 3 on real devices
/// configure it: every screen but the game master console must stay as it was.
/// </summary>
public sealed class FaultInjectionTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _scratch = new();
    private readonly TempDirectory _packs = new();
    private WebApplicationFactory<Program>? _factory;

    public FaultInjectionTests()
    {
        // The only pack of the directory, chosen at once: two rounds of a question, "Oui" (A) being the correct answer.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Échauffement", "Finale"));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory!.Services.GetRequiredService<GameLoop>();

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        _scratch.Dispose();
        _packs.Dispose();
    }

    [Fact]
    public async Task RoundIntent_FaultInjectedThreeTimes_TellsTheGameMasterAloneWhoSkipsTheRoundAndPlaysTheNext()
    {
        // Given: the answers of the players fail three times
        Start("Development");
        await using var display = await HubClients.ConnectAsync(_factory!);
        await AnnounceAsync(display, Role.Display, code: null);
        await using var gameMaster = await HubClients.ConnectAsync(_factory!);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);
        var phones = new List<HubConnection>();
        foreach (var nickname in new[] { "Zoé", "Max", "Léa" })
        {
            var phone = await HubClients.ConnectAsync(_factory!);
            phones.Add(phone);
            Assert.Null((await phone.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct)).Refusal);
        }

        await using var zoe = phones[0];
        await using var max = phones[1];
        await using var lea = phones[2];
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        Assert.Null((await gameMaster.StartGameAndFirstRoundAsync(Game, Ct)).Refusal);
        var first = Game.State.CurrentRound!.Id;
        await ShowQuestionAsync(gameMaster, first);
        await FlushAsync(display, gameMaster, zoe);
        var shown = Game.State;
        var counts = (Display: toDisplay.Display.Count, Phone: toZoe.Player.Count);

        // When: each player answers
        var failures = 0;
        foreach (var phone in phones)
        {
            await PlayerIntents.SendAsync(phone, clientSeq: 1, new QuizSubmitAnswer(first, 1, QuizChoiceLetter.A));
            failures++;
            await FlushAsync(gameMaster);
            Assert.Equal(failures, Assert.Single(toGameMaster.Incidents[^1].Incidents).Count);
        }

        // Then: nothing changed for the players and the TV screen, the game master is offered to skip the round
        await FlushAsync(display, zoe);
        Assert.Same(shown, Game.State);
        Assert.Equal(counts, (toDisplay.Display.Count, toZoe.Player.Count));
        Assert.Equal(IncidentCode.RoundHandlerFailed, toGameMaster.Incidents[^1].Incidents[0].Code);
        Assert.Equal(IncidentJournal.SkipThreshold, failures);
        Assert.Equal([first], toGameMaster.Incidents[^1].FailingRounds);

        // When: the game master skips it, and starts the next one
        await gameMaster.InvokeAsync(GameHub.SkipRound, Message(new SkipRoundRequest(first)), Ct);
        await gameMaster.InvokeAsync(GameHub.NextRound, Message(new NextRoundRequest(first)), Ct);
        var final = Game.State.CurrentRound!.Id;
        await gameMaster.InvokeAsync(GameHub.StartRound, Message(new StartRoundRequest(final)), Ct);
        await ShowQuestionAsync(gameMaster, final);
        await PlayerIntents.SendAsync(zoe, clientSeq: 2, new QuizSubmitAnswer(final, 1, QuizChoiceLetter.A));

        // Then: it plays normally
        var answers = Assert.IsType<QuizRound>(Game.State.CurrentRound!.State).Answers;
        Assert.Equal(QuizChoiceLetter.A, answers[Game.State.Players[0].Id].Choice);
        Secret[] secrets =
        [
            new(nameof(IncidentCode.RoundHandlerFailed), Audience.AllButGameMaster),
            new(nameof(IncidentList.FailingRounds), Audience.AllButGameMaster),
        ];
        await FlushAsync(display, zoe);
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Messages, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Messages, secrets);
        Assert.Contains(
            LoggedEvent.ReadAll(_scratch),
            e => e.Level == "Warning" && e.Template.StartsWith("Fault injection active", StringComparison.Ordinal) && e.Line.Contains("PlayerRoundInput", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Startup_FaultInjectionRequestedInProduction_IsIgnoredWithAWarning()
    {
        // Given
        Start("Production");
        await using var zoe = await HubClients.ConnectAsync(_factory!);
        Assert.Null((await zoe.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct)).Refusal);
        await using var gameMaster = await HubClients.ConnectAsync(_factory!);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);
        Assert.Null((await gameMaster.StartGameAndFirstRoundAsync(Game, Ct)).Refusal);
        var round = Game.State.CurrentRound!.Id;
        await ShowQuestionAsync(gameMaster, round);

        // When
        await PlayerIntents.SendAsync(zoe, clientSeq: 1, new QuizSubmitAnswer(round, 1, QuizChoiceLetter.A));

        // Then
        Assert.Single(Assert.IsType<QuizRound>(Game.State.CurrentRound!.State).Answers);
        Assert.Contains(
            LoggedEvent.ReadAll(_scratch),
            e => e.Level == "Warning" && e.Template.StartsWith("Fault injection requested by {Setting} but ignored", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Startup_FaultInjectedOnAnUnknownInput_ExitsNamingTheInputs()
    {
        // When
        using var server = ServerProcess.Start(new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["Network__Port"] = ServerProcess.GetFreePort().ToString(CultureInfo.InvariantCulture),
            ["LogFiles__Directory"] = _scratch.Path,
            ["Persistence__Directory"] = _scratch.Path,
            ["FaultInjection__FailOnInput"] = "PlayerAnswer",
        });
        var exitCode = await server.WaitForExitAsync(TimeSpan.FromSeconds(30));

        // Then
        Assert.Equal(1, exitCode);
        Assert.Contains("FaultInjection:FailOnInput names no input: PlayerAnswer", server.Output, StringComparison.Ordinal);
        Assert.Contains("PlayerRoundInput", server.Output, StringComparison.Ordinal);
    }

    private void Start(string environment) =>
        _ = (_factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment(environment)
            .UseScratchDirectory(_scratch.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path)
            .UseSetting("FaultInjection:FailOnInput", "PlayerRoundInput")
            .UseSetting("FaultInjection:FailCount", "3"))).Services;

    /// <summary>Shows the first question and its two choices: the answers open.</summary>
    private static async Task ShowQuestionAsync(HubConnection gameMaster, RoundId round)
    {
        GameMasterRoundIntent[] steps = [new QuizShowQuestion(round, 1), new QuizShowChoice(round, 1, QuizChoiceLetter.A), new QuizShowChoice(round, 1, QuizChoiceLetter.B)];
        foreach (var step in steps)
        {
            await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, Message(step), Ct);
        }
    }

    private static JsonElement Message<T>(T message) => JsonSerializer.SerializeToElement(message, ContractJsonOptions.Default);

    private static async Task AnnounceAsync(HubConnection connection, Role role, string? code) =>
        Assert.Null((await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(role, code), Ct)).Refusal);

    private async Task FlushAsync(params HubConnection[] connections)
    {
        foreach (var connection in connections)
        {
            await HubClients.FlushAsync<GameHub>(_factory!, connection);
        }
    }
}
