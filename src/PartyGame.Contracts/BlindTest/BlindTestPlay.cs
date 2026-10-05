namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// The game master plays the excerpt of the track announced: the TV screen plays it, and the buzzer opens on every phone,
/// at the same instant.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="TrackNumber">The track to play, from 1: once played, the intent sent again is obsolete.</param>
public sealed record BlindTestPlay(RoundId RoundId, int TrackNumber) : GameMasterRoundIntent(RoundId);
