using PartyGame.Contracts.Buzzer;

namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// What a blind test round shows on the phone of one player: their buzzer, then who has the hand. The music plays on the
/// TV screen alone: the view holds no audio.
/// </summary>
/// <param name="TrackNumber">The track in progress, from 1.</param>
/// <param name="TrackCount">How many tracks the round has.</param>
/// <param name="Opening">The current opening of the buzzer, which a buzz names.</param>
/// <param name="OpensAt">
/// When the current opening of the buzzer starts, as the music does, in server time, in milliseconds since the Unix epoch,
/// or <see langword="null"/> before the excerpt is played: the buzzer shows closed until then.
/// </param>
/// <param name="Buzzer">What the buzzer of this player shows.</param>
/// <param name="Winner">The nickname of the player who has the hand, or <see langword="null"/>.</param>
public sealed record BlindTestPlayerView(
    int TrackNumber,
    int TrackCount,
    int Opening,
    long? OpensAt,
    BuzzerButtonState Buzzer,
    string? Winner) : PlayerRoundView;
