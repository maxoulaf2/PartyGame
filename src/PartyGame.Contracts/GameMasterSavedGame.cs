using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// The game a restarted server found saved, as the game master console describes it before they resume it or start a new
/// one. Never its questions, its answers nor its players.
/// </summary>
/// <param name="GameId">The game found, which the decision of the game master names, so that a second one is obsolete.</param>
/// <param name="SavedAt">When it was last saved, in milliseconds since the Unix epoch on the clock of the server.</param>
/// <param name="Phase">The phase it stopped in.</param>
/// <param name="PackTitle">The title of the pack it plays, or of the pack chosen in its lobby, if any.</param>
/// <param name="Round">
/// The round in progress when it stopped, or the last one played between two rounds and once finished, or
/// <see langword="null"/> before the first round.
/// </param>
/// <param name="Step">
/// The step of <paramref name="Round"/> in progress when it stopped, such as the question of a quiz, or
/// <see langword="null"/> outside a round and when its game mode cannot tell.
/// </param>
/// <param name="PlayerCount">The number of players registered, at least 1: a game without player is not offered.</param>
/// <param name="MissingMedia">
/// The media files of its pack missing from the disk, as written in the descriptor: the game cannot be resumed until they
/// are back. Empty when none is.
/// </param>
public sealed record GameMasterSavedGame(
    GameId GameId,
    long SavedAt,
    Phase Phase,
    string? PackTitle,
    RoundInfo? Round,
    RoundStep? Step,
    int PlayerCount,
    ImmutableArray<string> MissingMedia);
