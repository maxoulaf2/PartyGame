using System.Collections.Immutable;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.BlindTest;
using PartyGame.Engine.Projections;
using PartyGame.Tests.Shared.Leaks;
using EngineBuzzer = PartyGame.Engine.Buzzers.Buzzer;

namespace PartyGame.Engine.Tests.Modes.BlindTest;

/// <summary>
/// Builds the blind test rounds and the games that play them, with the real blind test mode.
/// </summary>
internal static class BlindTestGames
{
    public static readonly BlindTestMode Mode = new();

    public static readonly GameModes Modes = new([Mode]);

    public static readonly GameEngine Engine = new(Modes);

    public static readonly Snapshots Snapshots = new(Modes);

    public const string PackId = "blindtest";

    /// <summary>A track played from its start, named after its title as an author would.</summary>
    public static BlindTestTrack Ode { get; } = new()
    {
        Excerpt = new AudioExcerpt { File = new MediaPath("sons/hymne-a-la-joie.mp3"), Duration = 20 },
        Title = "L'Hymne à la joie",
        Artist = "Ludwig van Beethoven",
    };

    /// <summary>A track played from the middle, without artist, with an image.</summary>
    public static BlindTestTrack Moon { get; } = new()
    {
        Excerpt = new AudioExcerpt { File = new MediaPath("sons/au-clair-de-la-lune.mp3"), Start = 9.5, Duration = 15 },
        Title = "Au clair de la lune",
        Image = new MediaPath("images/croissant.png"),
    };

    /// <summary>A track that comes after the others: no screen but the console may tell anything of it before its turn.</summary>
    public static BlindTestTrack Anthem { get; } = new()
    {
        Excerpt = new AudioExcerpt { File = new MediaPath("sons/la-marseillaise.mp3"), Start = 30, Duration = 10 },
        Title = "La Marseillaise",
        Artist = "Claude Joseph Rouget de Lisle",
    };

    public static BlindTestRoundDescriptor Round(params BlindTestTrack[] tracks) =>
        new() { Title = "Airs connus", Tracks = [.. tracks] };

    /// <summary>
    /// A game of a pack of the given round, with the given players, registered in order, whose round has just started.
    /// </summary>
    public static GameState Started(BlindTestRoundDescriptor round, params string[] nicknames)
    {
        ImmutableArray<RoundDescriptor> descriptors = [round];
        var pack = new CatalogPack(PackId, "Blind test", 1, new PackDescriptor { FormatVersion = 1, Title = "Blind test", Rounds = descriptors }, [])
        {
            Media = [.. round.Tracks.SelectMany(t => new[] { t.Excerpt.File, t.Image }).OfType<MediaPath>().Distinct()],
        };
        var lobby = Games.Accepted(Games.Accepted(Games.LobbyWith(nicknames), Games.Loaded(pack)), Games.Select(PackId));
        return Accepted(lobby, Games.Start());
    }

    /// <summary>
    /// The state after an input the test expects the blind test mode to accept, at <see cref="Games.Now"/> unless told
    /// otherwise.
    /// </summary>
    public static GameState Accepted(GameState state, GameInput input, DateTimeOffset? now = null)
    {
        var transition = Engine.Handle(state, input, At(now ?? Games.Now));
        Assert.Null(transition.Rejection);
        return transition.State;
    }

    public static GameContext At(DateTimeOffset now) => new(now, new Random(42));

    /// <summary>
    /// The same game, its round in progress on another track, as if the tracks before it were played.
    /// </summary>
    public static GameState AtTrack(GameState state, int trackIndex) =>
        state with { CurrentRound = state.CurrentRound! with { State = new BlindTestRound(RoundOf(state).Descriptor, trackIndex) } };

    /// <summary>The game master plays the excerpt of the track in progress.</summary>
    public static GameMasterRoundInput Play(GameState state, int? trackNumber = null) =>
        new(new BlindTestPlay(state.CurrentRound!.Id, trackNumber ?? RoundOf(state).TrackNumber), Games.Now);

    /// <summary>The game master judges the answer of the player who has the hand, on the current opening.</summary>
    public static GameMasterRoundInput Judge(GameState state, bool title = false, bool artist = false, int? trackNumber = null, int? opening = null) =>
        new(
            new BlindTestJudge(
                state.CurrentRound!.Id,
                trackNumber ?? RoundOf(state).TrackNumber,
                opening ?? RoundOf(state).Buzzer.Opening,
                title,
                artist),
            Games.Now);

    /// <summary>
    /// The same game, the given player having the hand, as <see cref="Answering"/> does, then judged: the music resumes
    /// while something is left to find.
    /// </summary>
    public static GameState Judged(GameState state, int player, bool title = false, bool artist = false)
    {
        state = Answering(state, (player, 1000));
        return Accepted(state, Judge(state, title, artist), Games.Now.AddSeconds(5));
    }

    /// <summary>
    /// The same game, buzzed by the given player on the reopened buzzer, 1 s after the music resumed, then arbitrated.
    /// </summary>
    public static GameState AnsweringAgain(GameState state, int player)
    {
        var pressedAt = StartsAt(state).AddSeconds(1);
        state = Accepted(state, Buzz(state, player, pressedAt), pressedAt);
        var elapsed = ArbitrationElapsed(state);
        return Accepted(state, elapsed, elapsed.DueAt);
    }

    /// <summary>The game master skips the track in progress.</summary>
    public static GameMasterRoundInput SkipTrack(GameState state, int? trackNumber = null) =>
        new(new BlindTestSkipTrack(state.CurrentRound!.Id, trackNumber ?? RoundOf(state).TrackNumber), Games.Now);

    /// <summary>
    /// A player buzzes on the current opening of the track in progress, pressed and received at the given time.
    /// </summary>
    public static PlayerRoundInput Buzz(GameState state, int player, DateTimeOffset pressedAt, int? trackNumber = null, int? opening = null) =>
        new(
            Games.PlayerIdOf(player),
            Games.NextClientSeq(state, player),
            new BlindTestBuzz(
                state.CurrentRound!.Id,
                trackNumber ?? RoundOf(state).TrackNumber,
                opening ?? RoundOf(state).Buzzer.Opening,
                pressedAt.ToUnixTimeMilliseconds()),
            pressedAt);

    /// <summary>The arbitration window of the buzzer in progress ends.</summary>
    public static TimerElapsed ArbitrationElapsed(GameState state) =>
        new(EngineBuzzer.ArbitrationTimer, RoundOf(state).Buzzer.ArbitrateAt!.Value) { RoundId = state.CurrentRound!.Id };

    /// <summary>When the music of the track in progress starts, once played.</summary>
    public static DateTimeOffset StartsAt(GameState state) => RoundOf(state).Playback.StartsAt!.Value;

    /// <summary>The same game, the excerpt of its track in progress played: the music starts, the buzzer opens.</summary>
    public static GameState Played(GameState state) => Accepted(state, Play(state));

    /// <summary>
    /// The same game, its excerpt played, then buzzed by the given players in this order, each pressed and received the
    /// given number of milliseconds after the music started: the arbitration window runs.
    /// </summary>
    public static GameState Buzzed(GameState state, params (int Player, int PressedAfter)[] buzzes)
    {
        state = Played(state);
        var startsAt = StartsAt(state);
        foreach (var (player, pressedAfter) in buzzes)
        {
            var pressedAt = startsAt.AddMilliseconds(pressedAfter);
            state = Accepted(state, Buzz(state, player, pressedAt), pressedAt);
        }

        return state;
    }

    /// <summary>
    /// The same game, buzzed as <see cref="Buzzed"/> does, then arbitrated at the end of the window: the earliest press
    /// has the hand, and the music is paused.
    /// </summary>
    public static GameState Answering(GameState state, params (int Player, int PressedAfter)[] buzzes)
    {
        state = Buzzed(state, buzzes);
        var elapsed = ArbitrationElapsed(state);
        return Accepted(state, elapsed, elapsed.DueAt);
    }

    /// <summary>
    /// The round in progress of a blind test game.
    /// </summary>
    public static BlindTestRound RoundOf(GameState state) => Assert.IsType<BlindTestRound>(state.CurrentRound?.State);

    /// <summary>
    /// What each viewer receives for a state, serialized as the hub does, the phones named by the nickname of their player.
    /// </summary>
    public static ProjectionSet Projected(GameState state) =>
        new(
            Snapshots.ForDisplay(state),
            Snapshots.ForGameMaster(state),
            [.. state.Players.Select(p => (p.Nickname, (object)Snapshots.ForPlayer(state, p)))],
            ContractJsonOptions.Default);
}
