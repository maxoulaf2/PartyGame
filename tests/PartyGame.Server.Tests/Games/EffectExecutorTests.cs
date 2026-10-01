using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
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
    private readonly RecordingLogger<EffectExecutor> _logger = new();
    private readonly TimerScheduler _timers;
    private readonly EffectExecutor _executor;

    public EffectExecutorTests()
    {
        _timers = new TimerScheduler(_inputs, _time, new RecordingLogger<TimerScheduler>());
        _executor = new EffectExecutor(_timers, _logger);
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
    public async Task ExecuteAsync_EffectWithoutExecutor_LogsError()
    {
        // When
        await _executor.ExecuteAsync(new TestEffect(), Ct);

        // Then
        var error = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, error.Level);
        Assert.Contains(nameof(TestEffect), error.Message, StringComparison.Ordinal);
    }
}
