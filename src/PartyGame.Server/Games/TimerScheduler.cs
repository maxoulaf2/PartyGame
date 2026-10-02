using PartyGame.Engine;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Server.Games;

/// <summary>
/// Executes the timer effects of the engine. A timer changes nothing by itself: when it elapses, it enqueues a
/// <see cref="TimerElapsed"/> input that the loop handles like any other.
/// </summary>
/// <remarks>
/// Scheduling and cancelling come from the loop, while timers elapse on the thread pool: the lock only guards the table of
/// pending timers, never the game state.
/// </remarks>
internal sealed class TimerScheduler(IGameInputWriter inputs, TimeProvider timeProvider, ILogger<TimerScheduler> logger)
    : IDisposable
{
    private readonly Lock _gate = new();
    private readonly Dictionary<TimerId, PendingTimer> _pending = [];
    private bool _disposed;

    /// <summary>
    /// Schedules a timer, replacing any pending timer with the same identifier. A due time already reached enqueues the
    /// input right away.
    /// </summary>
    public ValueTask ScheduleAsync(ScheduleTimer request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var delay = request.DueAt - timeProvider.GetUtcNow();
        lock (_gate)
        {
            if (_disposed)
            {
                // The application is stopping: nobody reads the queue anymore.
                return ValueTask.CompletedTask;
            }

            Remove(request.TimerId);
            if (delay > TimeSpan.Zero)
            {
                // Created under the lock: a timer that elapses at once waits until it is in the table, then finds itself there.
                var pending = new PendingTimer(request);
                pending.Timer = timeProvider.CreateTimer(OnElapsed, pending, delay, Timeout.InfiniteTimeSpan);
                _pending.Add(request.TimerId, pending);
                return ValueTask.CompletedTask;
            }
        }

        return inputs.WriteAsync(Elapsed(request), cancellationToken);
    }

    /// <summary>
    /// Cancels a pending timer. Without effect when no timer has this identifier, for instance because it already elapsed.
    /// </summary>
    public void Cancel(TimerId timerId)
    {
        lock (_gate)
        {
            Remove(timerId);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            foreach (var pending in _pending.Values)
            {
                pending.Timer?.Dispose();
            }

            _pending.Clear();
        }
    }

    private static TimerElapsed Elapsed(ScheduleTimer request) => new(request.TimerId, request.DueAt) { RoundId = request.RoundId };

    private void Remove(TimerId timerId)
    {
        if (_pending.Remove(timerId, out var previous))
        {
            previous.Timer?.Dispose();
        }
    }

    private void OnElapsed(object? state)
    {
        var elapsed = (PendingTimer)state!;
        lock (_gate)
        {
            // A timer cancelled or replaced while its callback was already starting must not report anything.
            if (!_pending.TryGetValue(elapsed.Request.TimerId, out var current) || current != elapsed)
            {
                return;
            }

            _pending.Remove(elapsed.Request.TimerId);
        }

        elapsed.Timer?.Dispose();
        _ = EnqueueAsync(elapsed.Request);
    }

    private async Task EnqueueAsync(ScheduleTimer request)
    {
        try
        {
            await inputs.WriteAsync(Elapsed(request), CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The loop is stopped: the input has nobody to be handled by.
        }
        catch (Exception ex)
        {
            logger.TimerInputLost(ex, request.TimerId.Value);
        }
    }

    private sealed class PendingTimer(ScheduleTimer request)
    {
        public ScheduleTimer Request { get; } = request;

        public ITimer? Timer { get; set; }
    }
}
