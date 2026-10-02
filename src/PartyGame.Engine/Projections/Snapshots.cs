using PartyGame.Contracts;
using PartyGame.Engine.Lobby;

namespace PartyGame.Engine.Projections;

/// <summary>
/// Projects the state of the game for each role. The state itself never reaches a client: what a projection leaves out
/// cannot leak, whoever opens the TV page or the developer tools of a phone.
/// </summary>
public static class Snapshots
{
    /// <summary>
    /// What the TV screen shows: public information only.
    /// </summary>
    /// <param name="state">The current state.</param>
    public static DisplaySnapshot ForDisplay(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new DisplaySnapshot(
            state.GameId,
            state.Version,
            PhaseOf(state),
            state.JoinAddress,
            [.. state.Players.Select(p => new DisplayPlayer(p.Id, p.Nickname, p.IsConnected))]);
    }

    /// <summary>
    /// What the game master console shows.
    /// </summary>
    /// <param name="state">The current state.</param>
    public static GameMasterSnapshot ForGameMaster(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new GameMasterSnapshot(
            state.GameId,
            state.Version,
            PhaseOf(state),
            [.. state.Players.Select(p => new GameMasterPlayer(p.Id, p.Nickname, p.IsConnected))],
            Launch.MinimumPlayerCount,
            state.JoinAddress,
            [.. state.JoinAddressCandidates.Select(c => new GameMasterJoinAddress(c.Address, c.InterfaceName))]);
    }

    /// <summary>
    /// What the phone of <paramref name="player"/> shows: public information and what is meant for this player only.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="player">A player registered in <paramref name="state"/>.</param>
    public static PlayerSnapshot ForPlayer(GameState state, Player player)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(player);
        return new PlayerSnapshot(state.GameId, state.Version, PhaseOf(state), player.Id, player.Nickname, state.Players.Length);
    }

    private static Phase PhaseOf(GameState state) => state.Phase switch
    {
        GamePhase.Lobby => Phase.Lobby,
        GamePhase.Started => Phase.Started,
        _ => throw new InvalidOperationException($"Phase {state.Phase} has no projection."),
    };
}
