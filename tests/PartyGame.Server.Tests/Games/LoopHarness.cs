using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Effects;
using PartyGame.Server.Games;

namespace PartyGame.Server.Tests.Games;

/// <summary>
/// A started <see cref="GameLoop"/> with recording collaborators, stopped on disposal.
/// </summary>
internal sealed class LoopHarness : IAsyncDisposable
{
    public static readonly GameState InitialState = GameState.Create(new GameId(Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff")), "192.168.1.42", [], new PackCatalog("packs", []));

    private LoopHarness(
        ScriptedEngine engine,
        Func<LoopHarness, IEffectExecutor>? effects,
        Func<Effect, bool>? effectFails,
        bool incidentsFail,
        IEnumerable<Func<Journal, RecordingListener>> listeners)
    {
        Engine = engine;
        Incidents = new RecordingIncidentReporter(incidentsFail);
        Listeners = [.. listeners.Select(create => create(Journal))];
        Loop = new GameLoop(
            InitialState,
            seed: 42,
            Inputs,
            engine,
            Time,
            effects?.Invoke(this) ?? new RecordingEffectExecutor(Journal, effectFails),
            Listeners,
            Incidents,
            Logger);
    }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero));

    public Journal Journal { get; } = new();

    public GameInputQueue Inputs { get; } = new();

    public RecordingLogger<GameLoop> Logger { get; } = new();

    public ScriptedEngine Engine { get; }

    public IReadOnlyList<RecordingListener> Listeners { get; }

    public RecordingIncidentReporter Incidents { get; }

    public GameLoop Loop { get; }

    public static async Task<LoopHarness> StartAsync(
        ScriptedEngine? engine = null,
        Func<Effect, bool>? effectFails = null,
        Func<LoopHarness, IEffectExecutor>? effects = null,
        bool incidentsFail = false,
        params Func<Journal, RecordingListener>[] listeners)
    {
        var harness = new LoopHarness(
            engine ?? new ScriptedEngine(),
            effects,
            effectFails,
            incidentsFail,
            listeners.Length == 0 ? [journal => new RecordingListener(journal)] : listeners);
        await harness.Loop.StartAsync(TestContext.Current.CancellationToken);
        return harness;
    }

    public async ValueTask DisposeAsync()
    {
        await Loop.StopAsync(CancellationToken.None);
        Loop.Dispose();
    }
}
