using PartyGame.Engine.Effects;

namespace PartyGame.Server.Games;

/// <summary>
/// Executes, outside the engine, the effects of a transition. Called by the loop only, one effect at a time.
/// </summary>
internal interface IEffectExecutor
{
    /// <summary>
    /// Executes an effect. It must stay quick: anything slow is started, not awaited, and reports back through the queue.
    /// </summary>
    ValueTask ExecuteAsync(Effect effect, CancellationToken cancellationToken);
}
