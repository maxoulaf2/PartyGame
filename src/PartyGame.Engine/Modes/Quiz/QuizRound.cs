namespace PartyGame.Engine.Modes.Quiz;

/// <summary>
/// State of a quiz round.
/// </summary>
/// <remarks>
/// Empty until the questions are played (US-E08-02): the round finishes as soon as it starts.
/// </remarks>
public sealed record QuizRound : RoundState;
