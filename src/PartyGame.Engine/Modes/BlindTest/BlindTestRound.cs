namespace PartyGame.Engine.Modes.BlindTest;

/// <summary>
/// State of a blind test round.
/// </summary>
/// <remarks>
/// Empty until the tracks are played (US-E15-02): the round finishes as soon as it starts.
/// </remarks>
public sealed record BlindTestRound : RoundState;
