using PartyGame.Contracts;

namespace PartyGame.Server.Hubs;

/// <summary>
/// The open connections of each player, kept out of the game state: only the moments a player gets their first
/// connection or loses their last one matter to the game, and the hub reports those to the loop.
/// </summary>
internal sealed class PlayerConnections
{
    private readonly Lock _gate = new();
    private readonly Dictionary<PlayerId, HashSet<string>> _connections = [];

    /// <summary>Records an open connection of a player. Returns whether it is the only one.</summary>
    public bool Add(PlayerId playerId, string connectionId)
    {
        lock (_gate)
        {
            if (!_connections.TryGetValue(playerId, out var connections))
            {
                connections = [];
                _connections.Add(playerId, connections);
            }

            return connections.Add(connectionId) && connections.Count == 1;
        }
    }

    /// <summary>Forgets a closed connection of a player. Returns whether it was the last one.</summary>
    public bool Remove(PlayerId playerId, string connectionId)
    {
        lock (_gate)
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
}
