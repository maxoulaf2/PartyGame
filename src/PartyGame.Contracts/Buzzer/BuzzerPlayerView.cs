namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// What a round of buzzer questions shows on the phone of one player.
/// </summary>
/// <remarks>
/// Empty until the questions are played (US-E13-04): the round finishes as soon as it starts.
/// </remarks>
public sealed record BuzzerPlayerView : PlayerRoundView;
