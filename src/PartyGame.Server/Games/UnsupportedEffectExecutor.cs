using PartyGame.Engine.Effects;

namespace PartyGame.Server.Games;

/// <summary>
/// Placeholder until timers are executed (US-E03-03): the engine emits no effect yet, so any effect is a bug worth logging.
/// </summary>
internal sealed class UnsupportedEffectExecutor(ILogger<UnsupportedEffectExecutor> logger) : IEffectExecutor
{
    public ValueTask ExecuteAsync(Effect effect, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(effect);
        logger.EffectNotSupported(effect.GetType().Name);
        return ValueTask.CompletedTask;
    }
}
