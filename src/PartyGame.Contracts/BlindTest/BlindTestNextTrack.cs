namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// The game master moves on from the revealed track: the next track of the round is announced, or the round ends after
/// its last one.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="TrackNumber">
/// The number of the revealed track to move on from, from 1: the intent is obsolete once the round has moved on, so that
/// a request sent twice, or by two consoles, never skips a track.
/// </param>
public sealed record BlindTestNextTrack(RoundId RoundId, int TrackNumber) : GameMasterRoundIntent(RoundId);
