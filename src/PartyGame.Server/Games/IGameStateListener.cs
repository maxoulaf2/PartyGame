using PartyGame.Engine.State;

namespace PartyGame.Server.Games;

/// <summary>
/// Notified by the loop after each transition that changed the state, once its effects are executed.
/// The broadcast of the snapshots (<see cref="Hubs.SnapshotBroadcaster"/>) is one.
/// </summary>
internal interface IGameStateListener
{
    /// <summary>
    /// Called from the loop, one change at a time. It must stay quick: the loop waits for it before the next input.
    /// </summary>
    ValueTask OnStateChangedAsync(GameState state, CancellationToken cancellationToken);
}
