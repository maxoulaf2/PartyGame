namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// The game master reveals the title and the artist of the track played, whoever has the hand: the music stops, the
/// buzzer closes, and the elements found earn their points.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="TrackNumber">The track to reveal, from 1: once revealed, the intent sent again is obsolete.</param>
public sealed record BlindTestRevealAnswer(RoundId RoundId, int TrackNumber) : GameMasterRoundIntent(RoundId);
