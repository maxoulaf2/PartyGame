namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// The game master skips the track in progress, without points, for instance when the TV screen cannot play it: the next
/// track is announced, or the round ends after its last one.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="TrackNumber">
/// The track to skip, from 1: the intent is obsolete once the round has moved on, so that a request sent twice, or by two
/// consoles, never skips two tracks.
/// </param>
public sealed record BlindTestSkipTrack(RoundId RoundId, int TrackNumber) : GameMasterRoundIntent(RoundId);
