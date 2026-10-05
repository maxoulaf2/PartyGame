namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// The game master judges the answer the player who has the hand gave out loud: the title and the artist apart, each found
/// one earning its points. Whatever they found, the player may not buzz again on the track, and the music resumes for the
/// others while something is left to find.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="TrackNumber">The track judged, from 1.</param>
/// <param name="Opening">
/// The opening of the buzzer the player won, as the console tells it: a judgment sent twice, or by two consoles, is
/// obsolete once the buzzer opens anew, and never judges the next winner.
/// </param>
/// <param name="TitleFound">Whether the player gave the title.</param>
/// <param name="ArtistFound">Whether the player gave the artist: never for a track without artist.</param>
public sealed record BlindTestJudge(RoundId RoundId, int TrackNumber, int Opening, bool TitleFound, bool ArtistFound) : GameMasterRoundIntent(RoundId);
