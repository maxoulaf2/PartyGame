namespace PartyGame.Contracts;

/// <summary>
/// The game master shows a step of the pack previewed on the TV screen. The request names the step to show rather than a
/// move, so that a double tap or two consoles show it once.
/// </summary>
/// <param name="RoundNumber">The round of the pack, from 1.</param>
/// <param name="StepNumber">The step of that round, from 1, such as the number of a question.</param>
/// <param name="PlayExcerpt">
/// Whether the TV screen plays the excerpt of the step, from its start and for its duration, when it has one.
/// </param>
public sealed record ShowPreviewStepRequest(int RoundNumber, int StepNumber, bool PlayExcerpt);
