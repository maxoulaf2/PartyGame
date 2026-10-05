namespace PartyGame.Engine.Modes.Buzzer;

/// <summary>
/// State of a round of buzzer questions.
/// </summary>
/// <remarks>
/// Empty until the questions are played (US-E13-04): the round finishes as soon as it starts.
/// </remarks>
public sealed record BuzzerRound : RoundState;
