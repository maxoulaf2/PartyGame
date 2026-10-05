using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// What the TV screen shows. Public by construction: anybody on the network can open the TV page, so it never holds
/// an answer before its reveal nor any secret.
/// </summary>
/// <param name="GameId">The game the snapshot belongs to. A client that receives another one starts over.</param>
/// <param name="Version">Increases by one with each change of the game, so that a client ignores older snapshots.</param>
/// <param name="Phase">Current phase of the game.</param>
/// <param name="JoinAddress">
/// The IPv4 address phones join at, encoded in the QR code, or <see langword="null"/> when the server knows none. The
/// client adds the port of the page it loaded, which is the one phones must use as well.
/// </param>
/// <param name="Players">Registered players, in order of arrival, so that nobody moves when another one joins.</param>
/// <param name="PackTitle">
/// The title of the pack chosen for the game, or <see langword="null"/> while none is. The only thing the TV screen
/// knows of the packs: never their list, nor their content before it is played.
/// </param>
/// <param name="Round">
/// The round in progress, or the round that just finished between two rounds and once the game is finished, or
/// <see langword="null"/> before the first round.
/// </param>
/// <param name="RoundView">
/// What the game mode of the round in progress shows on the TV screen, or <see langword="null"/> outside a round.
/// </param>
/// <param name="Ranking">
/// The players by rank, then in alphabetical order of nickname within a rank, between two rounds and once the game is
/// finished; empty otherwise. Every registered player is ranked, but those who joined once the game was finished.
/// </param>
/// <param name="JoinCodeShown">
/// Whether the game master asked to show the QR code over the game, outside the lobby, which always shows it.
/// </param>
public sealed record DisplaySnapshot(
    GameId GameId,
    long Version,
    Phase Phase,
    string? JoinAddress,
    ImmutableArray<DisplayPlayer> Players,
    string? PackTitle,
    RoundInfo? Round,
    DisplayRoundView? RoundView,
    ImmutableArray<RankedPlayer> Ranking,
    bool JoinCodeShown);
