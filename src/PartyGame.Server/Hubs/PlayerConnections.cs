using PartyGame.Contracts;
using PartyGame.Engine.Inputs;
using PartyGame.Server.Games;

namespace PartyGame.Server.Hubs;

/// <summary>
/// The open connections of each player, kept out of the game state: only the moments a player gets their first
/// connection or loses their last one matter to the game, and only those are reported to the loop.
/// </summary>
/// <remarks>
/// The input of a change is queued under the same lock as the count: a tab that closes while another one resumes the
/// session could otherwise queue <see cref="PlayerConnectionLost"/> after <see cref="PlayerConnectionRestored"/>, and
/// leave a connected player shown disconnected.
/// </remarks>
internal sealed class PlayerConnections(IGameInputWriter inputs)
{
    private readonly Lock _gate = new();
    private readonly Dictionary<PlayerId, HashSet<string>> _connections = [];

    /// <summary>
    /// Records the connection a player registered on. Nothing is reported: the registration itself shows them connected.
    /// </summary>
    public void Register(PlayerId playerId, string connectionId)
    {
        lock (_gate)
        {
            Add(playerId, connectionId);
        }
    }

    /// <summary>
    /// Records a connection on which a player resumed their session, and reports them connected again if it is their only
    /// one.
    /// </summary>
    /// <exception cref="OperationCanceledException">The loop is stopped.</exception>
    public async ValueTask ResumeAsync(PlayerId playerId, string connectionId)
    {
        ValueTask written;
        lock (_gate)
        {
            if (!Add(playerId, connectionId))
            {
                return;
            }

            // Queued before the lock is released (see IGameInputWriter.WriteAsync); only the wait happens outside.
            written = inputs.WriteAsync(new PlayerConnectionRestored(playerId), CancellationToken.None);
        }

        await written.ConfigureAwait(false);
    }

    /// <summary>
    /// Forgets a closed connection of a player, and reports them disconnected if it was their last one. Returns whether it
    /// was.
    /// </summary>
    /// <exception cref="OperationCanceledException">The loop is stopped.</exception>
    public async ValueTask<bool> DisconnectAsync(PlayerId playerId, string connectionId)
    {
        ValueTask written;
        lock (_gate)
        {
            if (!Remove(playerId, connectionId))
            {
                return false;
            }

            written = inputs.WriteAsync(new PlayerConnectionLost(playerId), CancellationToken.None);
        }

        await written.ConfigureAwait(false);
        return true;
    }

    /// <summary>Returns whether the connection is the only one of the player.</summary>
    private bool Add(PlayerId playerId, string connectionId)
    {
        if (!_connections.TryGetValue(playerId, out var connections))
        {
            connections = [];
            _connections.Add(playerId, connections);
        }

        return connections.Add(connectionId) && connections.Count == 1;
    }

    /// <summary>Returns whether the connection was the last one of the player.</summary>
    private bool Remove(PlayerId playerId, string connectionId)
    {
        if (!_connections.TryGetValue(playerId, out var connections) || !connections.Remove(connectionId))
        {
            return false;
        }

        if (connections.Count > 0)
        {
            return false;
        }

        _connections.Remove(playerId);
        return true;
    }
}
