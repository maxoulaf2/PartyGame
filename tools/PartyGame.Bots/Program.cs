using PartyGame.Bots;

const string Usage = """
    Usage : dotnet run --project tools/PartyGame.Bots -- --url <adresse> [--count <n>] [--behavior <comportements>] [--gm-code <code> [--pack <id>]]
      --behavior : random, fast, slow, silent ou flaky (random par défaut), ou un mélange : random:6,flaky:2,silent:2
      --gm-code  : un bot GM mène la partie seul ; sans lui, elle se pilote depuis la console GM
    """;

Uri? url = null;
int? count = null;
var behaviorSpec = "random";
string? gmCode = null;
string? packId = null;
for (var i = 0; i < args.Length; i++)
{
    var value = i + 1 < args.Length ? args[i + 1] : null;
    switch (args[i])
    {
        case "--url" when Uri.TryCreate(value, UriKind.Absolute, out var parsed):
            url = parsed;
            break;
        case "--count" when int.TryParse(value, out var parsed) && parsed > 0:
            count = parsed;
            break;
        case "--behavior" when value is not null:
            behaviorSpec = value;
            break;
        case "--gm-code" when value is not null:
            gmCode = value;
            break;
        case "--pack" when value is not null:
            packId = value;
            break;
        default:
            Console.Error.WriteLine($"Option invalide : {args[i]}");
            Console.Error.WriteLine(Usage);
            return 2;
    }

    i++;
}

if (url is null || BotBehaviors.Parse(behaviorSpec, count) is not { } behaviors)
{
    Console.Error.WriteLine(Usage);
    return 2;
}

using var stopping = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stopping.Cancel();
};

var stats = new BotStats();
var players = behaviors.Select((behavior, index) => new BotPlayer(url, $"Bot {index + 1:00}", behavior, stats)).ToList();
await using var gameMaster = gmCode is null ? null : new BotGameMaster(url, gmCode, packId, players.Count, TimeSpan.FromSeconds(2));
string? failure = null;

List<Task> running = [.. players.Select(player => GuardAsync(player.RunAsync)), GuardAsync(ReportAsync)];
if (gameMaster is not null)
{
    running.Add(GuardAsync(async cancellationToken =>
    {
        await gameMaster.RunAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine("Partie terminée : classement final affiché. Ctrl+C pour arrêter les bots.");
    }));
}

Console.WriteLine($"{players.Count} bots vers {url}. Ctrl+C pour arrêter.");
try
{
    await Task.WhenAll(running).ConfigureAwait(false);
}
finally
{
    // Disconnected properly, so that the server shows them away at once.
    foreach (var player in players)
    {
        await player.DisposeAsync().ConfigureAwait(false);
    }
}

Console.WriteLine(Summary());
if (failure is not null)
{
    Console.Error.WriteLine(failure);
    return 1;
}

return 0;

async Task GuardAsync(Func<CancellationToken, Task> run)
{
    try
    {
        await run(stopping.Token).ConfigureAwait(false);
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested)
    {
    }
    catch (BotException ex)
    {
        Interlocked.CompareExchange(ref failure, ex.Message, null);
        await stopping.CancelAsync().ConfigureAwait(false);
    }
}

async Task ReportAsync(CancellationToken cancellationToken)
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        Console.WriteLine(Summary());
    }
}

string Summary() => stats.Summary(players.Count(player => player.IsConnected), players.Count);
