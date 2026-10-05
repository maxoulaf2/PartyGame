namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// What a round of buzzer questions shows on the game master console.
/// </summary>
/// <remarks>
/// Empty until the questions are played (US-E13-04): the round finishes as soon as it starts.
/// </remarks>
public sealed record BuzzerGameMasterView : GameMasterRoundView;
