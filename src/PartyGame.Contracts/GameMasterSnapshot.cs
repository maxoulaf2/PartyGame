using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// What the game master console shows. The only projection allowed to hold the answers before their reveal; it never
/// holds the game master code nor the tokens of the players.
/// </summary>
/// <param name="GameId">The game the snapshot belongs to. A client that receives another one starts over.</param>
/// <param name="Version">Increases by one with each change of the game, so that a client ignores older snapshots.</param>
/// <param name="Phase">Current phase of the game.</param>
/// <param name="Players">Registered players, in order of arrival, so that nobody moves when another one joins.</param>
/// <param name="MinimumPlayerCount">
/// Number of registered players, connected or not, below which the game cannot be started.
/// </param>
/// <param name="JoinAddress">
/// The address currently encoded in the QR code of the TV screen, or <see langword="null"/> when the server knows none.
/// </param>
/// <param name="JoinAddressCandidates">
/// The addresses the game master may choose from instead, best first. Only this projection holds them: the TV screen
/// gets the chosen address alone.
/// </param>
/// <param name="PackCatalog">
/// The packs the game master may choose from, with the problems of the invalid ones, in the lobby only: once the game is
/// started, its pack is fixed and the catalog is <see langword="null"/>.
/// </param>
/// <param name="SelectedPackId">
/// The identifier of the pack chosen for the game, or <see langword="null"/> while none is: the game cannot start then.
/// </param>
/// <param name="PackTitle">The title of the pack chosen for the game, or <see langword="null"/> while none is.</param>
/// <param name="Round">
/// The round in progress, or the round that just finished between two rounds and once the game is finished, or
/// <see langword="null"/> before the first round.
/// </param>
/// <param name="RoundView">
/// What the game mode of the round in progress shows on the console, or <see langword="null"/> outside a round.
/// </param>
/// <param name="Ranking">
/// The players by rank, then in alphabetical order of nickname within a rank, between two rounds and once the game is
/// finished; empty otherwise. Every registered player is ranked, but those who joined once the game was finished.
/// </param>
/// <param name="NextRoundTitle">
/// The title of the round the game master starts next, between two rounds; <see langword="null"/> otherwise. Only this
/// projection holds it: the others discover the rounds as they are played.
/// </param>
/// <param name="SavedGame">
/// The game the server found saved when it restarted, for the game master to resume it or start a new one, while the
/// phase is <see cref="Phase.ResumePending"/>; <see langword="null"/> otherwise. Only this projection describes it.
/// </param>
/// <param name="RoundSkipped">
/// Whether the game master skipped <paramref name="Round"/>, once it is over: the points of its question in progress were
/// not awarded. Only this projection tells it: for the others, the round is over like any other.
/// </param>
/// <param name="JoinCodeShown">
/// Whether the TV screen shows the QR code over the game, at the request of the game master, outside the lobby.
/// </param>
/// <param name="Preview">The step of a pack the TV screen previews, in the lobby, or <see langword="null"/>.</param>
public sealed record GameMasterSnapshot(
    GameId GameId,
    long Version,
    Phase Phase,
    ImmutableArray<GameMasterPlayer> Players,
    int MinimumPlayerCount,
    string? JoinAddress,
    ImmutableArray<GameMasterJoinAddress> JoinAddressCandidates,
    GameMasterPackCatalog? PackCatalog,
    string? SelectedPackId,
    string? PackTitle,
    RoundInfo? Round,
    GameMasterRoundView? RoundView,
    ImmutableArray<RankedPlayer> Ranking,
    string? NextRoundTitle,
    bool RoundSkipped,
    GameMasterSavedGame? SavedGame,
    bool JoinCodeShown,
    GameMasterPreview? Preview);
