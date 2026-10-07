using System.Collections.Immutable;
using System.Text.Json.Serialization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Lobby;
using PartyGame.Engine.Packs;

namespace PartyGame.Engine.State;

/// <summary>
/// Complete state of a game. Immutable: the engine derives a new state with <c>with</c> and never modifies the one it receives.
/// It is serializable as is, to persist the game after each transition.
/// </summary>
/// <param name="GameId">Identifier of the game.</param>
/// <param name="Version">
/// Number of the state, carried by every snapshot. The game loop increments it with each transition that changes the
/// state, never the engine: it is persisted with the state, so that a game resumed after a crash goes on counting.
/// </param>
/// <param name="Phase">Current phase of the game.</param>
/// <param name="JoinAddress">
/// The IPv4 address phones join at, shown on the TV screen, or <see langword="null"/> when the server knows none. Part of
/// the state so that a change of address is broadcast like any other change.
/// </param>
/// <param name="JoinAddressCandidates">
/// The addresses the game master may choose <paramref name="JoinAddress"/> from, best first, detected by the server at
/// startup. Only the game master sees them.
/// </param>
/// <param name="Players">Registered players, in order of arrival.</param>
/// <param name="PlayerTokens">The player each token identifies. Secret: no projection ever contains it.</param>
/// <param name="Catalog">
/// The packs the game master may choose from, loaded by the server at startup and on each reload. Part of the state so
/// that a reload is broadcast like any other change. Only the game master sees it, and only in the lobby.
/// </param>
/// <param name="SelectedPackId">
/// The valid pack of <paramref name="Catalog"/> chosen for the game, or <see langword="null"/> while none is.
/// </param>
/// <param name="Pack">
/// The descriptor of the pack the game plays, copied from <paramref name="Catalog"/> when the game starts: from then on, the
/// game depends neither on the catalog nor on the disk. <see langword="null"/> in the lobby.
/// </param>
/// <param name="Media">
/// The media files of <paramref name="Pack"/> by their identifier, drawn when the game starts: the server serves them by
/// these identifiers only. Empty in the lobby. Secret: a projection contains the URL of a media file, never its path.
/// </param>
/// <param name="CurrentRound">
/// The round in progress, or the last one played between two rounds and once the game is finished, or
/// <see langword="null"/> before the first round.
/// </param>
/// <remarks>
/// The state of a round is proper to its game mode: <see cref="GameStateJson"/> declares the derived types of the
/// registered modes.
/// </remarks>
public sealed record GameState(
    GameId GameId,
    long Version,
    GamePhase Phase,
    string? JoinAddress,
    ImmutableArray<JoinAddressCandidate> JoinAddressCandidates,
    ImmutableArray<Player> Players,
    ImmutableDictionary<PlayerToken, PlayerId> PlayerTokens,
    PackCatalog Catalog,
    string? SelectedPackId,
    PackDescriptor? Pack,
    PackMedia Media,
    PlayedRound? CurrentRound)
{
    /// <summary>
    /// The activities of the pack the game plays, each as one round, in this order. Empty in the lobby.
    /// </summary>
    [JsonIgnore] // read from the pack, which is persisted
    public ImmutableArray<RoundDescriptor> Rounds => Pack?.Rounds ?? [];

    /// <summary>
    /// Whether the game is started and not finished yet: a round is announced, in progress, or just finished.
    /// </summary>
    [JsonIgnore] // read from the phase, which is persisted
    public bool IsInProgress => Phase is GamePhase.RoundIntro or GamePhase.Round or GamePhase.BetweenRounds;

    /// <summary>
    /// The game found saved when the server restarted, while <see cref="Phase"/> is <see cref="GamePhase.ResumePending"/>;
    /// <see langword="null"/> otherwise. Secret: only the game master gets a description of it.
    /// </summary>
    public PendingGame? PendingGame { get; init; }

    /// <summary>
    /// Whether the game master asked the TV screen to show the QR code outside the lobby, which always shows it. Absent
    /// from a game saved before it existed, which then hides it.
    /// </summary>
    public bool JoinCodeShown { get; init; }

    /// <summary>
    /// The code each player types to join again from another phone or browser, drawn by the hub at registration. Secret:
    /// only the game master sees it. Absent from a game saved before it existed, whose players then have none.
    /// </summary>
    public ImmutableDictionary<PlayerId, string> ReconnectionCodes { get; init; } = ImmutableDictionary<PlayerId, string>.Empty;

    /// <summary>
    /// The pack the game master previews on the TV screen, in the lobby only, or <see langword="null"/>. Only the TV screen
    /// and the game master see it.
    /// </summary>
    [JsonIgnore] // a preview is no game: a restarted server never resumes it
    public PackPreview? Preview { get; init; }

    /// <summary>
    /// When the last round ended, once the game is finished: the screens reveal the podium step by step from then on.
    /// Absent from a game saved before it existed, whose podium shows at once.
    /// </summary>
    public DateTimeOffset? FinishedAt { get; init; }

    /// <summary>
    /// When the game master paused the game, or <see langword="null"/> while it is not paused. The phase and the round stay
    /// as they were: the resumption moves the deadlines of the round on by the length of the pause.
    /// </summary>
    public DateTimeOffset? PausedAt { get; init; }

    /// <summary>
    /// The programme of the rounds, around <see cref="CurrentRound"/>; <see cref="RoundSchedule.Empty"/> in the lobby. Only
    /// the game master sees it. Absent from a game saved before it existed: the loader then follows the order of the pack.
    /// </summary>
    public RoundSchedule Schedule { get; init; } = RoundSchedule.Empty;

    /// <summary>
    /// Creates the state of a new game: a lobby without any player, at version 1. When the catalog holds a single valid
    /// pack, it is already chosen.
    /// </summary>
    /// <param name="gameId">Identifier of the game, generated by the caller since the engine has no randomness of its own.</param>
    /// <param name="joinAddress">The address phones join at, chosen by the server at startup, if any.</param>
    /// <param name="joinAddressCandidates">The addresses the game master may choose from instead, best first.</param>
    /// <param name="catalog">The packs loaded by the server at startup.</param>
    public static GameState Create(
        GameId gameId,
        string? joinAddress,
        ImmutableArray<JoinAddressCandidate> joinAddressCandidates,
        PackCatalog catalog) =>
        PackChoice.WithCatalog(
            new(
                gameId,
                Version: 1,
                GamePhase.Lobby,
                joinAddress,
                joinAddressCandidates,
                [],
                ImmutableDictionary<PlayerToken, PlayerId>.Empty,
                catalog,
                SelectedPackId: null,
                Pack: null,
                PackMedia.Empty,
                CurrentRound: null),
            catalog);

    /// <summary>
    /// Creates the state of a server that found a saved game: it waits for the game master to resume it or to start a new
    /// game, which is the lobby of <see cref="Create"/>. Its version is that of the game found, so that the snapshots of
    /// the resumed game go on counting from it.
    /// </summary>
    /// <param name="gameId">Identifier of the new game, should the game master start one.</param>
    /// <param name="joinAddress">The address phones join at, chosen by the server at startup, if any.</param>
    /// <param name="joinAddressCandidates">The addresses the game master may choose from instead, best first.</param>
    /// <param name="catalog">The packs loaded by the server at startup.</param>
    /// <param name="pending">The game found.</param>
    public static GameState CreateResumePending(
        GameId gameId,
        string? joinAddress,
        ImmutableArray<JoinAddressCandidate> joinAddressCandidates,
        PackCatalog catalog,
        PendingGame pending)
    {
        ArgumentNullException.ThrowIfNull(pending);
        return Create(gameId, joinAddress, joinAddressCandidates, catalog) with
        {
            Version = pending.Game.Version,
            Phase = GamePhase.ResumePending,
            PendingGame = pending,
        };
    }
}
