namespace PartyGame.Engine.Modes.OpenQuestion;

/// <summary>
/// State of a round of open questions.
/// </summary>
/// <remarks>
/// Empty until the questions are played (US-E16-02): the round finishes as soon as it starts.
/// </remarks>
public sealed record OpenQuestionRound : RoundState;
