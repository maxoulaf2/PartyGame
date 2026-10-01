using PartyGame.Engine.Effects;

namespace PartyGame.Server.Games;

/// <summary>
/// Hands each effect of a transition to the service that executes it.
/// </summary>
internal sealed class EffectExecutor(TimerScheduler timers, ILogger<EffectExecutor> logger) : IEffectExecutor
{
    public ValueTask ExecuteAsync(Effect effect, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(effect);

        switch (effect)
        {
            case ScheduleTimer schedule:
                return timers.ScheduleAsync(schedule, cancellationToken);

            case CancelTimer cancel:
                timers.Cancel(cancel.TimerId);
                return ValueTask.CompletedTask;

            default:
                // An effect without executor is a bug of the server, not of the game: it is logged and the game goes on.
                logger.EffectNotSupported(effect.GetType().Name);
                return ValueTask.CompletedTask;
        }
    }
}
