using PartyGame.Contracts;
using PartyGame.Engine.Lobby;
using PartyGame.Engine.Modes;

namespace PartyGame.Engine.Projections;

/// <summary>
/// Projects the state of the game for each role. The state itself never reaches a client: what a projection leaves out
/// cannot leak, whoever opens the TV page or the developer tools of a phone. During a round, the game mode of the round
/// projects its own view for each role.
/// </summary>
/// <param name="modes">The game modes that play the rounds of the packs.</param>
public sealed class Snapshots(GameModes modes)
{
    /// <summary>
    /// What the TV screen shows: public information only.
    /// </summary>
    /// <param name="state">The current state.</param>
    public DisplaySnapshot ForDisplay(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new DisplaySnapshot(
            state.GameId,
            state.Version,
            PhaseOf(state),
            state.JoinAddress,
            [.. state.Players.Select(p => new DisplayPlayer(p.Id, p.Nickname, p.IsConnected))],
            PackTitleOf(state),
            RoundInfoOf(state),
            RoundInProgress(state) is var (mode, round) ? mode.ProjectForDisplay(round.State, state) : null);
    }

    /// <summary>
    /// What the game master console shows.
    /// </summary>
    /// <param name="state">The current state.</param>
    public GameMasterSnapshot ForGameMaster(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new GameMasterSnapshot(
            state.GameId,
            state.Version,
            PhaseOf(state),
            [.. state.Players.Select(p => new GameMasterPlayer(p.Id, p.Nickname, p.IsConnected))],
            Launch.MinimumPlayerCount,
            state.JoinAddress,
            [.. state.JoinAddressCandidates.Select(c => new GameMasterJoinAddress(c.Address, c.InterfaceName))],
            state.Phase == GamePhase.Lobby ? CatalogOf(state.Catalog) : null,
            state.SelectedPackId,
            PackTitleOf(state),
            RoundInfoOf(state),
            RoundInProgress(state) is var (mode, round) ? mode.ProjectForGameMaster(round.State, state) : null);
    }

    /// <summary>
    /// What the phone of <paramref name="player"/> shows: public information and what is meant for this player only.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="player">A player registered in <paramref name="state"/>.</param>
    public PlayerSnapshot ForPlayer(GameState state, Player player)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(player);
        return new PlayerSnapshot(
            state.GameId,
            state.Version,
            PhaseOf(state),
            player.Id,
            player.Nickname,
            state.Players.Length,
            RoundInfoOf(state),
            RoundInProgress(state) is var (mode, round) ? mode.ProjectForPlayer(round.State, state, player) : null);
    }

    private static Phase PhaseOf(GameState state) => state.Phase switch
    {
        GamePhase.Lobby => Phase.Lobby,
        GamePhase.Round => Phase.Round,
        GamePhase.BetweenRounds => Phase.BetweenRounds,
        GamePhase.Finished => Phase.Finished,
        _ => throw new InvalidOperationException($"Phase {state.Phase} has no projection."),
    };

    /// <summary>
    /// The title of the pack the game plays, or of the pack chosen in the lobby.
    /// </summary>
    private static string? PackTitleOf(GameState state) =>
        state.Pack?.Title ?? (state.SelectedPackId is { } id ? state.Catalog.Find(id)?.Title : null);

    /// <summary>
    /// What the game master needs to choose a pack: the rounds of each one, never its questions nor its answers, and why
    /// the invalid ones cannot be chosen.
    /// </summary>
    private static GameMasterPackCatalog CatalogOf(PackCatalog catalog) =>
        new(
            catalog.Directory,
            [
                .. catalog.Packs.Select(pack => new GameMasterPack(
                    pack.Id,
                    pack.Title,
                    pack.RoundCount,
                    pack.IsValid,
                    pack.IsValid ? [.. pack.Descriptor.Rounds.Select(r => new GameMasterPackRound(r.Title, GameModes.TypeOf(r)))] : [],
                    pack.Problems)),
            ]);

    private static RoundInfo? RoundInfoOf(GameState state) =>
        state.CurrentRound is { } round
            ? new RoundInfo(round.Id, round.Index + 1, state.Rounds.Length, state.Rounds[round.Index].Title)
            : null;

    /// <summary>
    /// The round in progress and its game mode, which alone knows what each role may see of it. Between two rounds, the
    /// last round played has no view anymore.
    /// </summary>
    private (IGameMode Mode, PlayedRound Round)? RoundInProgress(GameState state) =>
        state is { Phase: GamePhase.Round, CurrentRound: { } round } ? (modes.For(state.Rounds[round.Index]), round) : null;
}
