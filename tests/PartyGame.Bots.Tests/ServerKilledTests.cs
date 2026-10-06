using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;
using PartyGame.Server.Tests;

namespace PartyGame.Bots.Tests;

/// <summary>
/// The real server, in a process of its own, killed as a crash would kill it while players answer, then started again on
/// the same data folder: what the players were told is recorded must still be, and nothing may count twice.
/// </summary>
public sealed class ServerKilledTests : IDisposable
{
    private const string FirstCode = "135790";
    private const string SecondCode = "246802";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    private static readonly string[] _nicknames = ["Zoé", "Max", "Léa", .. Enumerable.Range(1, 10).Select(n => $"Bot {n:00}")];

    private readonly TempDirectory _scratch = new();
    private readonly string _packs;
    private readonly int _port = ServerProcess.GetFreePort();

    public ServerKilledTests()
    {
        // A single question, "Oui" (A) being the correct answer. Its countdown never starts: the choice B is never shown.
        var pack = Directory.CreateDirectory(Path.Combine(_scratch.Path, "packs", "bots"));
        _packs = pack.Parent!.FullName;
        var question = new { text = "Question ?", choices = new[] { new { text = "Oui", correct = true }, new { text = "Non", correct = false } } };
        var round = new { type = "quiz", title = "Manche", answerSeconds = 120, questions = new[] { question } };
        File.WriteAllText(Path.Combine(pack.FullName, "pack.json"), JsonSerializer.Serialize(new { formatVersion = 1, title = "Soirée des bots", rounds = new[] { round } }));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Uri ServerUrl => new($"http://127.0.0.1:{_port}");

    public void Dispose() => _scratch.Dispose();

    [Fact]
    public async Task Restart_KilledDuringABurstOfAnswers_LosesNoAcknowledgedAnswerAndCountsNoneTwice()
    {
        // Given: three players and ten bots, each answering within a few seconds of the first choice shown
        using var firstRun = await StartServerAsync(FirstCode);
        var stats = new BotStats();
        var players = _nicknames
            .Select(nickname => new BotPlayer(ServerUrl, nickname, BotBehavior.Random, stats))
            .ToList();
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var playing = players.Select(player => player.RunAsync(stopping.Token)).ToList();
        try
        {
            RoundId round;
            await using (var console = await ConsoleOf.ConnectAsync(ServerUrl, FirstCode))
            {
                await console.WaitForAsync(snapshot => snapshot.Players.Length == players.Count);
                Assert.Null((await console.Connection.InvokeAsync<SelectPackResult>("SelectPack", new SelectPackRequest("bots"), Ct)).Refusal);
                Assert.Null((await console.Connection.InvokeAsync<StartGameResult>("StartGame", Ct)).Refusal);
                round = (await console.WaitForAsync(snapshot => snapshot.Phase == Phase.RoundIntro)).Round!.RoundId;
                await console.Connection.InvokeAsync("StartRound", new StartRoundRequest(round), Ct);
                await console.WaitForAsync(snapshot => snapshot.Phase == Phase.Round);
                await console.SendAsync(new QuizShowQuestion(round, 1));

                // When: the server is killed in the middle of the answers, some acknowledged, others on their way
                await console.SendAsync(new QuizShowChoice(round, 1, QuizChoiceLetter.A));
                await WaitUntilAsync(() => players.Count(player => player.Acknowledged.Count > 0) >= players.Count / 2);
                firstRun.Kill();
            }

            // Read once the bots lost their connection: an acknowledgment sent just before the kill may arrive after it.
            await WaitUntilAsync(() => players.All(player => !player.IsConnected));
            var acknowledged = players.Where(player => player.Acknowledged.Count > 0).Select(player => player.Nickname).ToList();

            // And: started again, the game master resumes the game
            using var secondRun = await StartServerAsync(SecondCode);
            await using var resumed = await ConsoleOf.ConnectAsync(ServerUrl, SecondCode);
            var pending = await resumed.WaitForAsync(snapshot => snapshot.SavedGame is not null);
            var resolvedFrom = pending.Version;
            await resumed.Connection.InvokeAsync("ResolveSavedGame", Message(new ResolveSavedGameRequest(pending.SavedGame!.GameId, Resume: true)), Ct);

            // Then: the game resumed holds every answer acknowledged before the kill, before any phone came back
            var first = await resumed.WaitForAsync(snapshot => snapshot.Version > resolvedFrom && snapshot.RoundView is QuizGameMasterView);
            var saved = ((QuizGameMasterView)first.RoundView!).Answers.Where(answer => answer.Choice is not null).Select(answer => answer.Nickname);
            Assert.Subset(saved.ToHashSet(), acknowledged.ToHashSet());

            // And: once every phone is back and sent again what it never had acknowledged, each answer counts once
            await resumed.WaitForAsync(snapshot => snapshot.RoundView is QuizGameMasterView view && view.Answers.All(answer => answer.Choice is not null));
            await resumed.SendAsync(new QuizShowChoice(round, 1, QuizChoiceLetter.B));
            await resumed.WaitForAsync(snapshot => snapshot.RoundView is QuizGameMasterView { Phase: QuizQuestionPhase.Locked });
            await resumed.SendAsync(new QuizRevealAnswer(round, 1));
            var revealed = (QuizGameMasterView)(await resumed.WaitForAsync(snapshot => snapshot.RoundView is QuizGameMasterView { Phase: QuizQuestionPhase.Revealed })).RoundView!;
            Assert.Equal((players.Count, players.Count), (revealed.Answers.Length, revealed.Answers.DistinctBy(answer => answer.PlayerId).Count()));
            Assert.All(revealed.Answers, answer => Assert.Equal((QuizChoiceLetter?)QuizChoiceLetter.A, answer.Choice));
            Assert.All(revealed.Answers, answer => Assert.True(answer.Points > 0, answer.Nickname));
        }
        finally
        {
            await stopping.CancelAsync();
            foreach (var player in players)
            {
                await player.DisposeAsync();
            }
        }

        Assert.All(playing, task => Assert.False(task.IsFaulted, task.Exception?.ToString()));
    }

    private async Task<ServerProcess> StartServerAsync(string code)
    {
        var server = ServerProcess.Start(new Dictionary<string, string>
        {
            ["Network__Port"] = _port.ToString(CultureInfo.InvariantCulture),
            ["GameMaster__Code"] = code,
            ["Packs__Directory"] = _packs,
            ["Persistence__Directory"] = _scratch.Path,
            ["LogFiles__Directory"] = _scratch.Path,
        });
        using var http = new HttpClient();
        var health = await server.WaitForResponseAsync(http, new Uri(ServerUrl, "/health"), _timeout);
        Assert.True(health.IsSuccessStatusCode, server.Output);
        return server;
    }

    private static JsonElement Message<T>(T message) => JsonSerializer.SerializeToElement(message, ContractJsonOptions.Default);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + _timeout;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out.");
            await Task.Delay(50, Ct);
        }
    }

    /// <summary>A game master console, which keeps every snapshot it receives.</summary>
    private sealed class ConsoleOf : IAsyncDisposable
    {
        private readonly List<GameMasterSnapshot> _snapshots = [];

        private ConsoleOf(HubConnection connection)
        {
            Connection = connection;
            connection.On<GameMasterSnapshot>(nameof(IGameClient.ReceiveGameMasterSnapshot), snapshot =>
            {
                lock (_snapshots)
                {
                    _snapshots.Add(snapshot);
                }
            });
        }

        public HubConnection Connection { get; }

        public static async Task<ConsoleOf> ConnectAsync(Uri serverUrl, string code)
        {
            var console = new ConsoleOf(BotHub.Create(serverUrl, configure: null));
            await console.Connection.StartAsync(Ct);
            var announced = await console.Connection.InvokeAsync<AnnouncementResult>("Announce", new Announcement(Role.GameMaster, code), Ct);
            Assert.Null(announced.Refusal);
            return console;
        }

        /// <summary>The first snapshot received that matches, waited for if none does yet.</summary>
        public async Task<GameMasterSnapshot> WaitForAsync(Func<GameMasterSnapshot, bool> match)
        {
            GameMasterSnapshot? found = null;
            await WaitUntilAsync(() =>
            {
                lock (_snapshots)
                {
                    found = _snapshots.FirstOrDefault(match);
                }

                return found is not null;
            });
            return found!;
        }

        public Task SendAsync(GameMasterRoundIntent intent) => Connection.InvokeAsync("SendGameMasterRoundIntent", Message(intent), Ct);

        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }
}
