using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;
using PartyGame.Server.Games;
using PartyGame.Server.Tests.Persistence;

namespace PartyGame.Server.Tests.Games;

public sealed class TimerSchedulerTests : IDisposable
{
    private static readonly TimerId _countdown = new("countdown");

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero));
    private readonly RecordingInputWriter _inputs = new();
    private readonly RecordingLogger<TimerScheduler> _logger = new();
    private readonly TimerScheduler _timers;

    public TimerSchedulerTests()
    {
        _timers = new TimerScheduler(_inputs, _time, _logger);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _timers.Dispose();

    [Fact]
    public async Task ScheduleAsync_TimeReachesDueAt_EnqueuesTimerElapsedOnce()
    {
        // Given
        var dueAt = _time.GetUtcNow().AddSeconds(10);
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, dueAt), Ct);

        // When
        _time.Advance(TimeSpan.FromSeconds(10) - TimeSpan.FromMilliseconds(1));
        var beforeDueAt = _inputs.Inputs;
        _time.Advance(TimeSpan.FromMilliseconds(1));
        _time.Advance(TimeSpan.FromMinutes(1));

        // Then
        Assert.Empty(beforeDueAt);
        Assert.Equal([new TimerElapsed(_countdown, dueAt)], _inputs.Inputs);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(0)]
    public async Task ScheduleAsync_TimerOfARound_EnqueuesTimerElapsedOfThatRound(int delaySeconds)
    {
        // Given: a timer the engine marked with its round, due later or already
        var round = new RoundId(Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff"));
        var dueAt = _time.GetUtcNow().AddSeconds(delaySeconds);

        // When
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, dueAt) { RoundId = round }, Ct);
        _time.Advance(TimeSpan.FromMinutes(1));

        // Then
        Assert.Equal([new TimerElapsed(_countdown, dueAt) { RoundId = round }], _inputs.Inputs);
    }

    [Fact]
    public async Task Cancel_BeforeDueAt_EnqueuesNothing()
    {
        // Given
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, _time.GetUtcNow().AddSeconds(10)), Ct);

        // When
        _timers.Cancel(_countdown);
        _time.Advance(TimeSpan.FromMinutes(1));

        // Then
        Assert.Empty(_inputs.Inputs);
    }

    [Fact]
    public void Cancel_UnknownTimer_DoesNothing()
    {
        // When
        var exception = Record.Exception(() => _timers.Cancel(_countdown));

        // Then
        Assert.Null(exception);
        Assert.Empty(_inputs.Inputs);
    }

    [Fact]
    public async Task CancelRound_TimersOfSeveralRounds_CancelsThoseOfThatRoundOnly()
    {
        // Given: two timers of the skipped round, one of another round, one of the engine itself
        var skipped = new RoundId(Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff"));
        var other = new RoundId(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"));
        var dueAt = _time.GetUtcNow().AddSeconds(10);
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, dueAt) { RoundId = skipped }, Ct);
        await _timers.ScheduleAsync(new ScheduleTimer(new TimerId("reveal"), dueAt) { RoundId = skipped }, Ct);
        await _timers.ScheduleAsync(new ScheduleTimer(new TimerId("other-round"), dueAt) { RoundId = other }, Ct);
        await _timers.ScheduleAsync(new ScheduleTimer(new TimerId("engine"), dueAt), Ct);

        // When
        _timers.CancelRound(skipped);
        _time.Advance(TimeSpan.FromMinutes(1));

        // Then
        Assert.Equal(
            ["engine", "other-round"],
            _inputs.Inputs.OfType<TimerElapsed>().Select(i => i.TimerId.Value).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task ScheduleAsync_IdentifierAlreadyPending_ReplacesPreviousTimer()
    {
        // Given
        var first = _time.GetUtcNow().AddSeconds(10);
        var second = _time.GetUtcNow().AddSeconds(20);
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, first), Ct);

        // When
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, second), Ct);
        _time.Advance(TimeSpan.FromSeconds(10));
        var atFirstDueAt = _inputs.Inputs;
        _time.Advance(TimeSpan.FromSeconds(10));

        // Then
        Assert.Empty(atFirstDueAt);
        Assert.Equal([new TimerElapsed(_countdown, second)], _inputs.Inputs);
    }

    [Fact]
    public async Task ScheduleAsync_DistinctIdentifiers_ElapseIndependently()
    {
        // Given
        var arbitration = new TimerId("arbitration");
        var countdownDueAt = _time.GetUtcNow().AddSeconds(10);
        var arbitrationDueAt = _time.GetUtcNow().AddMilliseconds(250);
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, countdownDueAt), Ct);
        await _timers.ScheduleAsync(new ScheduleTimer(arbitration, arbitrationDueAt), Ct);

        // When
        _time.Advance(TimeSpan.FromSeconds(10));

        // Then
        Assert.Equal([new TimerElapsed(arbitration, arbitrationDueAt), new TimerElapsed(_countdown, countdownDueAt)], _inputs.Inputs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ScheduleAsync_DueAtAlreadyReached_EnqueuesImmediately(int secondsFromNow)
    {
        // Given
        var dueAt = _time.GetUtcNow().AddSeconds(secondsFromNow);

        // When
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, dueAt), Ct);

        // Then
        Assert.Equal([new TimerElapsed(_countdown, dueAt)], _inputs.Inputs);
    }

    [Fact]
    public async Task ScheduleAsync_DueAtAlreadyReached_CancelsPendingTimerWithSameIdentifier()
    {
        // Given
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, _time.GetUtcNow().AddSeconds(10)), Ct);
        var now = _time.GetUtcNow();

        // When
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, now), Ct);
        _time.Advance(TimeSpan.FromMinutes(1));

        // Then
        Assert.Equal([new TimerElapsed(_countdown, now)], _inputs.Inputs);
    }

    [Fact]
    public async Task Dispose_PendingTimers_AreReleasedAndNewOnesIgnored()
    {
        // Given
        await _timers.ScheduleAsync(new ScheduleTimer(_countdown, _time.GetUtcNow().AddSeconds(10)), Ct);

        // When
        _timers.Dispose();
        await _timers.ScheduleAsync(new ScheduleTimer(new TimerId("late"), _time.GetUtcNow().AddSeconds(5)), Ct);
        _time.Advance(TimeSpan.FromMinutes(1));

        // Then
        Assert.Empty(_inputs.Inputs);
    }

    [Fact]
    public async Task OnElapsed_InputCannotBeEnqueued_LogsErrorAndKeepsScheduling()
    {
        // Given
        using var timers = new TimerScheduler(new RecordingInputWriter(fails: true), _time, _logger);
        await timers.ScheduleAsync(new ScheduleTimer(_countdown, _time.GetUtcNow().AddSeconds(10)), Ct);

        // When
        _time.Advance(TimeSpan.FromSeconds(10));
        var exception = await Record.ExceptionAsync(
            () => timers.ScheduleAsync(new ScheduleTimer(_countdown, _time.GetUtcNow().AddSeconds(10)), Ct).AsTask());

        // Then
        Assert.Null(exception);
        var error = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, error.Level);
        Assert.Contains(_countdown.Value, error.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(error.Exception);
    }

    [Fact]
    public async Task GameLoop_TransitionSchedulesTimer_HandlesTimerElapsedWhenTimeReachesDueAt()
    {
        // Given
        var dueAt = new DateTimeOffset(2026, 10, 1, 20, 0, 30, TimeSpan.Zero);
        var engine = new ScriptedEngine((state, input, context) => input switch
        {
            TimerElapsed elapsed => new Transition(state with { Players = state.Players.Add(new Player(default, elapsed.TimerId.Value, IsConnected: true)) }, []),
            TestInput { Value: 1 } => new Transition(state, [new ScheduleTimer(_countdown, dueAt)]),
            _ => ScriptedEngine.AddPlayer(state, input, context),
        });
        await using var harness = await LoopHarness.StartAsync(
            engine,
            effects: h => new EffectExecutor(new TimerScheduler(h.Inputs, h.Time, _logger), TestPersistence.In(Path.GetTempPath())));
        await harness.Inputs.SubmitAsync(new TestInput(1), Ct);

        // When
        harness.Time.Advance(TimeSpan.FromSeconds(30));
        await harness.Inputs.SubmitAsync(new TestInput(2), Ct);

        // Then
        Assert.Equal([_countdown.Value, "2"], harness.Loop.State.Players.Select(p => p.Nickname));
        Assert.Equal(new TimerElapsed(_countdown, dueAt), harness.Engine.Inputs[1]);
    }
}
