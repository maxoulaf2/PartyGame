using PartyGame.Contracts;
using PartyGame.Contracts.BlindTest;

namespace PartyGame.Bots;

/// <summary>
/// How the bots play the blind test: they buzz as on a buzzer question, once the music starts, and the game master judges
/// at random the title and the artist still to find.
/// </summary>
internal static class BlindTestBot
{
    /// <summary>Whether the snapshot shows the buzz recorded.</summary>
    public static bool Reflects(PlayerSnapshot snapshot, BlindTestBuzz buzz) =>
        snapshot.Round?.RoundId == buzz.RoundId
        && snapshot.RoundView is BlindTestPlayerView view
        && view.TrackNumber == buzz.TrackNumber
        && view.Opening == buzz.Opening
        && BuzzerBot.Pressed(view.Buzzer);

    /// <summary>
    /// What the game master does next: plays the excerpt, judges the player who has the hand at random, reveals the track
    /// when nobody buzzed for a while (<paramref name="stalled"/>), then moves to the next track.
    /// </summary>
    public static GameMasterRoundIntent? NextStep(RoundId roundId, BlindTestGameMasterView view, bool stalled, Random random) => view.Phase switch
    {
        BlindTestTrackPhase.Ready => new BlindTestPlay(roundId, view.TrackNumber),
        BlindTestTrackPhase.Listening when stalled => new BlindTestRevealAnswer(roundId, view.TrackNumber),
        BlindTestTrackPhase.Answering => new BlindTestJudge(
            roundId,
            view.TrackNumber,
            view.Opening,
            TitleFound: view.TitleFoundBy is null && random.Next(2) == 0,
            ArtistFound: view.Artist is not null && view.ArtistFoundBy is null && random.Next(2) == 0),
        BlindTestTrackPhase.Revealed => new BlindTestNextTrack(roundId, view.TrackNumber),
        _ => null,
    };
}
