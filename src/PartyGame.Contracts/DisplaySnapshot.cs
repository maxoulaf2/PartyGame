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
/// <param name="Preview">
/// The step of a pack the game master previews on the TV screen, in the lobby, or <see langword="null"/>. The phones never
/// receive it: they stay on the lobby.
/// </param>
/// <param name="FinishedAt">
/// When the game finished, in milliseconds since the Unix epoch on the clock of the server, once it is; <see langword="null"/>
/// otherwise, and for a game saved before it existed. The screens reveal the podium step by step from then on, all at
/// the same time, and a screen opened later knows where the reveal stands.
/// </param>
/// <param name="PausedAt">
/// When the game master paused the game, in milliseconds since the Unix epoch on the clock of the server, or
/// <see langword="null"/> while it is not paused. The countdowns of the screens stand still at that time, and the phones
/// show the pause instead of anything interactive.
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
    bool JoinCodeShown,
    DisplayPreview? Preview,
    long? FinishedAt,
    long? PausedAt);
