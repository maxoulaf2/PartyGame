using PartyGame.Engine.Effects;
using PartyGame.Server.Persistence;

namespace PartyGame.Server.Games;

/// <summary>
/// Hands each effect of a transition to the service that executes it.
/// </summary>
internal sealed class EffectExecutor(TimerScheduler timers, GamePersistence persistence) : IEffectExecutor
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

            case CancelRoundTimers cancelRound:
                timers.CancelRound(cancelRound.RoundId);
                return ValueTask.CompletedTask;

            case ArchiveSavedGame:
                persistence.ArchiveSavedGame();
                return ValueTask.CompletedTask;

            default:
                // A bug of the server, not of the game: the loop logs it, tells the game master, and the game goes on.
                throw new NotSupportedException($"Effect {effect.GetType().Name} has no executor.");
        }
    }
}
