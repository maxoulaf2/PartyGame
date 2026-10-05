using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes.BlindTest;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests.Modes.BlindTest;

/// <summary>
/// What the views of a blind test round may show to each viewer: the title and the artist to the game master only, the
/// excerpt to the TV screen and the game master only, under an opaque URL, the tracks to come to nobody else, who buzzed
/// during the arbitration to nobody but the player themselves, the time stamps of the buzzes and the paths of the files to
/// nobody.
/// </summary>
public sealed class BlindTestLeakTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly BlindTestRoundDescriptor _round =
        BlindTestGames.Round(BlindTestGames.Ode, BlindTestGames.Moon, BlindTestGames.Anthem);

    private static readonly LeakSuite<GameState, BlindTestPhase> _suite = new()
    {
        PhaseOf = state => BlindTestGames.RoundOf(state).Phase,
        Project = BlindTestGames.Projected,
        Scenarios =
        [
            ("first track announced", Started()),
            ("track without artist announced", BlindTestGames.AtTrack(Started(), 1)),
            ("last track announced", BlindTestGames.AtTrack(Started(), 2)),
            ("excerpt played", BlindTestGames.Played(Started())),
            ("excerpt from the middle played", BlindTestGames.Played(BlindTestGames.AtTrack(Started(), 1))),
            ("player joined once played", BlindTestGames.Accepted(BlindTestGames.Played(BlindTestGames.Started(_round, "Zoé", "Max")), Games.Join("Léa", player: 3))),
            ("one buzz, arbitrating", BlindTestGames.Buzzed(Started(), (2, 1000))),
            ("two buzzes, arbitrating", BlindTestGames.Buzzed(Started(), (2, 1000), (1, 1100))),
            ("winner designated, music paused", BlindTestGames.Answering(Started(), (2, 1000), (1, 1100))),
            ("winner designated once the excerpt ended", BlindTestGames.Answering(BlindTestGames.AtTrack(Started(), 1), (3, 25_000))),
            ("nothing found, music resumed", BlindTestGames.Judged(Started(), 2)),
            ("title found, music resumed", BlindTestGames.Judged(Started(), 2, title: true)),
            ("title found, another player has the hand", BlindTestGames.AnsweringAgain(BlindTestGames.Judged(Started(), 2, title: true), 3)),
            ("title and artist found", BlindTestGames.Judged(Started(), 2, title: true, artist: true)),
            ("title of a track without artist found", BlindTestGames.Judged(BlindTestGames.AtTrack(Started(), 1), 3, title: true)),
            ("revealed while the music plays", Revealed(BlindTestGames.Played(Started()))),
            ("revealed during the arbitration", Revealed(BlindTestGames.Buzzed(Started(), (2, 1000)))),
            ("revealed while a player has the hand", Revealed(BlindTestGames.Answering(Started(), (2, 1000)))),
            ("revealed once the title found", Revealed(BlindTestGames.Judged(Started(), 2, title: true))),
        ],
        SecretsOf = SecretsOf,
        Pairs =
        [
            TrackPair("title, announced", track => track with { Title = "Ode an die Freude" }, state => state),
            TrackPair("title, played", track => track with { Title = "Ode an die Freude" }, BlindTestGames.Played),
            TrackPair("title, winner designated", track => track with { Title = "Ode an die Freude" }, state => BlindTestGames.Answering(state, (1, 1000))),
            TrackPair("artist, announced", track => track with { Artist = "Friedrich von Schiller" }, state => state),
            TrackPair("artist, winner designated", track => track with { Artist = "Friedrich von Schiller" }, state => BlindTestGames.Answering(state, (1, 1000))),
            TrackPair("title, title found", track => track with { Title = "Ode an die Freude" }, state => BlindTestGames.Judged(state, 1, title: true)),
            TrackPair("artist, title found", track => track with { Artist = "Friedrich von Schiller" }, state => BlindTestGames.Judged(state, 1, title: true)),
            TrackPair("title and artist, revealed", track => track with { Title = "Ode an die Freude", Artist = "Friedrich von Schiller" }, state => BlindTestGames.Judged(state, 1, title: true, artist: true), Audience.Players),
            TrackPair("whether the track has an artist, played", track => track with { Artist = null }, BlindTestGames.Played),

            // Nobody learns who buzzed before the winner is designated, but the player who did.
            new SecretPair<GameState>(
                "whether Max buzzed too, arbitrating",
                BlindTestGames.Buzzed(Started(), (1, 1000), (2, 1100)),
                BlindTestGames.Buzzed(Started(), (1, 1000)),
                Audience.AllButGameMasterAnd("Max")),

            // The time stamps decide, and stay on the server: the same winner looks the same however fast the others were.
            new SecretPair<GameState>(
                "when Max pressed after Zoé, winner designated",
                BlindTestGames.Answering(Started(), (1, 1000), (2, 1050)),
                BlindTestGames.Answering(Started(), (1, 1000), (2, 1200)),
                Audience.AllButGameMasterAnd("Max")),
        ],
    };

    [Fact]
    public void BlindTestViews_EveryPhase_IsCoveredForEachRole() => _suite.AssertEveryPhaseIsCovered();

    [Fact]
    public void BlindTestViews_AnyScenario_ShowNoSecretToWhomItIsHiddenFrom() => _suite.AssertNoSecretIsShown();

    [Fact]
    public void BlindTestViews_WithoutTheSecrets_LookTheSameToWhomTheyAreHiddenFrom() => _suite.AssertPairsLookTheSame();

    private static GameState Started() => BlindTestGames.Started(_round, _players);

    private static GameState Revealed(GameState state) => BlindTestGames.Accepted(state, BlindTestGames.RevealAnswer(state));

    /// <summary>
    /// The same game, its first track told otherwise.
    /// </summary>
    private static SecretPair<GameState> TrackPair(string name, Func<BlindTestTrack, BlindTestTrack> change, Func<GameState, GameState> play, Audience? hiddenFrom = null) =>
        new(
            name,
            play(Started()),
            play(BlindTestGames.Started(_round with { Tracks = _round.Tracks.SetItem(0, change(BlindTestGames.Ode)) }, _players)),
            hiddenFrom ?? Audience.AllButGameMaster);

    private static IEnumerable<Secret> SecretsOf(GameState state)
    {
        var round = BlindTestGames.RoundOf(state);

        // The title, the artist and the image show on the TV screen at the reveal only, and never on the phones.
        var hiddenFrom = round.Phase == BlindTestPhase.Revealed ? Audience.Players : Audience.AllButGameMaster;
        yield return new Secret(round.Track.Title, hiddenFrom);
        if (round.Track.Artist is { } artist)
        {
            yield return new Secret(artist, hiddenFrom);
        }

        if (round.Track.Image is { } image)
        {
            yield return new Secret(state.Media.UrlOf(image), hiddenFrom);
        }

        // The music plays on the TV screen alone: no phone receives it.
        yield return new Secret(state.Media.UrlOf(round.Track.Excerpt.File), Audience.Players);

        // The tracks to come, their files included, are discovered as they are played.
        foreach (var track in round.Descriptor.Tracks.Skip(round.TrackIndex + 1))
        {
            yield return new Secret(track.Title, Audience.AllButGameMaster);
            yield return new Secret(state.Media.UrlOf(track.Excerpt.File), Audience.AllButGameMaster);
            if (track.Artist is { } next)
            {
                yield return new Secret(next, Audience.AllButGameMaster);
            }
        }

        // A file is named after what it plays or shows, which is the answer.
        foreach (var path in state.Media.Files.Values.Select(m => m.Value))
        {
            yield return new Secret(path, Audience.Everyone);
            foreach (var part in path.Split('/'))
            {
                yield return new Secret(Path.GetFileNameWithoutExtension(part), Audience.Everyone);
            }
        }

        // The winner is named on every screen, the other players on their phone alone.
        foreach (var player in state.Players)
        {
            if (player.Id != round.Buzzer.Winner)
            {
                yield return new Secret(player.Nickname, Audience.OtherPlayersThan(player.Nickname));
            }

            yield return new Secret(player.Id.Value.ToString(), Audience.OtherPlayersThan(player.Nickname));
        }
    }
}
