using PartyGame.Engine.Inputs;

namespace PartyGame.Server.Games;

/// <summary>
/// Write-only access to the queue of the game loop, for the producers of inputs (hub, timers).
/// Producers never touch the state: they only enqueue inputs, which the loop handles one at a time.
/// </summary>
internal interface IGameInputWriter
{
    /// <summary>
    /// Enqueues an input whose outcome the producer does not wait for. The input is queued before this method returns:
    /// inputs written one call after the other are handled in that order, even when their tasks are awaited later.
    /// </summary>
    /// <exception cref="OperationCanceledException">The loop is stopped, or <paramref name="cancellationToken"/> is cancelled.</exception>
    ValueTask WriteAsync(GameInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueues an input and waits until the loop has handled it, for intents that expect an answer (registration…).
    /// </summary>
    /// <exception cref="OperationCanceledException">The loop is stopped, or <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<InputOutcome> SubmitAsync(GameInput input, CancellationToken cancellationToken);
}
