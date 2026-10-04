using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Server.Games;

namespace PartyGame.Server.Tests.Games;

public sealed class EffectExecutorTests : IDisposable
{
    private static readonly TimerId _countdown = new("countdown");

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero));
    private readonly RecordingInputWriter _inputs = new();
    private readonly TimerScheduler _timers;
    private readonly EffectExecutor _executor;

    public EffectExecutorTests()
    {
        _timers = new TimerScheduler(_inputs, _time, new RecordingLogger<TimerScheduler>());
        _executor = new EffectExecutor(_timers);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _timers.Dispose();

    [Fact]
    public async Task ExecuteAsync_ScheduleTimer_SchedulesIt()
    {
        // Given
        var dueAt = _time.GetUtcNow().AddSeconds(10);

        // When
        await _executor.ExecuteAsync(new ScheduleTimer(_countdown, dueAt), Ct);
        _time.Advance(TimeSpan.FromSeconds(10));

        // Then
        Assert.Equal([new TimerElapsed(_countdown, dueAt)], _inputs.Inputs);
    }

    [Fact]
    public async Task ExecuteAsync_CancelTimer_CancelsIt()
    {
        // Given
        await _executor.ExecuteAsync(new ScheduleTimer(_countdown, _time.GetUtcNow().AddSeconds(10)), Ct);

        // When
        await _executor.ExecuteAsync(new CancelTimer(_countdown), Ct);
        _time.Advance(TimeSpan.FromSeconds(10));

        // Then
        Assert.Empty(_inputs.Inputs);
    }

    [Fact]
    public async Task ExecuteAsync_CancelRoundTimers_CancelsTheTimersOfTheRound()
    {
        // Given
        var round = new RoundId(Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff"));
        await _executor.ExecuteAsync(new ScheduleTimer(_countdown, _time.GetUtcNow().AddSeconds(10)) { RoundId = round }, Ct);

        // When
        await _executor.ExecuteAsync(new CancelRoundTimers(round), Ct);
        _time.Advance(TimeSpan.FromSeconds(10));

        // Then
        Assert.Empty(_inputs.Inputs);
    }

    [Fact]
    public async Task ExecuteAsync_EffectWithoutExecutor_ThrowsForTheLoopToReportIt()
    {
        // When
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => _executor.ExecuteAsync(new TestEffect(), Ct).AsTask());

        // Then
        Assert.Contains(nameof(TestEffect), exception.Message, StringComparison.Ordinal);
    }
}
