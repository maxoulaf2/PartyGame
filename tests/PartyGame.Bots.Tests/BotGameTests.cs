extern alias Server;

using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Server.Tests;
using ServerProgram = Server::Program;

namespace PartyGame.Bots.Tests;

/// <summary>
/// Bots playing a whole game against a real server, reached in memory, with the real quiz mode and the real clock.
/// </summary>
public sealed class BotGameTests : IAsyncDisposable
{
    private const string Code = "135790";

    private readonly TempDirectory _scratch = new();
    private readonly WebApplicationFactory<ServerProgram> _factory;

    public BotGameTests()
    {
        // Two rounds of a single question, "Oui" (A) being the correct answer: the fast bots always choose it.
        var packs = Directory.CreateDirectory(Path.Combine(_scratch.Path, "packs"));
        var pack = packs.CreateSubdirectory("bots");
        var rounds = Enumerable.Range(1, 2).Select(number => $"Manche {number}").Select(title => new
        {
            type = "quiz",
            title,
            answerSeconds = 5,
            questions = new[] { new { text = "Question ?", choices = new[] { new { text = "Oui", correct = true }, new { text = "Non", correct = false } } } },
        });
        File.WriteAllText(Path.Combine(pack.FullName, "pack.json"), JsonSerializer.Serialize(new { formatVersion = 1, title = "Soirée des bots", rounds }));

        _factory = new WebApplicationFactory<ServerProgram>().WithWebHostBuilder(builder => builder
            .UseSetting("LogFiles:Directory", _scratch.Path)
            .UseSetting("Persistence:Directory", _scratch.Path)
            .UseSetting("Packs:Directory", packs.FullName)
            .UseSetting("GameMaster:Code", Code));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Uri ServerUrl => _factory.Server.BaseAddress;

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _scratch.Dispose();
    }

    [Fact]
    public async Task RunAsync_GameMasterAndMixedBehaviors_PlayUpToFinalRanking()
    {
        // Given: a TV screen watching, a bot game master and ten bots of every behavior
        var displayed = new List<DisplaySnapshot>();
        await using var display = BotHub.Create(ServerUrl, InMemory);
        display.On<DisplaySnapshot>(nameof(IGameClient.ReceiveDisplaySnapshot), snapshot =>
        {
            lock (displayed)
            {
                displayed.Add(snapshot);
            }
        });
        await display.StartAsync(Ct);
        await display.InvokeAsync<AnnouncementResult>("Announce", new Announcement(Role.Display, null), Ct);

        var stats = new BotStats();
        BotBehavior[] behaviors = [.. BotBehaviors.Parse("random:3,fast:2,slow:2,silent:1,flaky:2", null)!];
        var players = behaviors
            .Select((behavior, index) => new BotPlayer(ServerUrl, $"Bot {index + 1:00}", behavior, stats, TimeSpan.FromSeconds(1), InMemory))
            .ToList();
        await using var gameMaster = new BotGameMaster(ServerUrl, Code, "bots", players.Count, TimeSpan.FromMilliseconds(100), InMemory);
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(Ct);

        try
        {
            // When
            var playing = players.Select(player => player.RunAsync(stopping.Token)).ToList();
            await gameMaster.RunAsync(stopping.Token).WaitAsync(TimeSpan.FromSeconds(90), Ct);

            // Then: every bot reaches the final ranking, flaky ones included once they are back
            await WaitUntilAsync(() => players.All(player => player.Snapshot?.Phase == Phase.Finished));
            Assert.All(players.Where(p => p.Behavior == BotBehavior.Fast), p => Assert.True(p.Snapshot!.Score > 0, p.Nickname));
            Assert.All(players.Where(p => p.Behavior == BotBehavior.Silent), p => Assert.Equal(0, p.Snapshot!.Score));
            Assert.True(stats.Reconnections > 0, "The flaky bots never came back.");
            Assert.All(playing, task => Assert.False(task.IsFaulted, task.Exception?.ToString()));
        }
        finally
        {
            await stopping.CancelAsync();
            foreach (var player in players)
            {
                await player.DisposeAsync();
            }
        }

        // And: no answer was ever counted twice, whatever the flaky bots sent again
        lock (displayed)
        {
            var views = displayed.Select(s => s.RoundView).OfType<QuizDisplayView>().ToList();
            Assert.NotEmpty(views);
            Assert.All(views, view => Assert.InRange(view.AnsweredCount, 0, view.ParticipantCount));
            Assert.All(
                views.Where(view => view.Reveal is not null),
                view => Assert.Equal(view.Reveal!.Answers.Length, view.Reveal.Answers.DistinctBy(answer => answer.PlayerId).Count()));
        }
    }

    private void InMemory(HttpConnectionOptions options)
    {
        var server = _factory.Server;
        options.Transports = HttpTransportType.WebSockets;
        options.HttpMessageHandlerFactory = _ => server.CreateHandler();
        options.WebSocketFactory = async (context, cancellationToken) =>
            await server.CreateWebSocketClient().ConnectAsync(context.Uri, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out.");
            await Task.Delay(50, Ct);
        }
    }
}
