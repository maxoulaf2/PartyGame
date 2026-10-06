namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master shows a step of the pack previewed on the TV screen. Only a connection authenticated as game master gets
/// it past the hub.
/// </summary>
/// <param name="RoundNumber">The round of the pack, from 1.</param>
/// <param name="StepNumber">The step of that round, from 1.</param>
/// <param name="PlayExcerpt">Whether the TV screen plays the excerpt of the step, when it has one.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record ShowPreviewStep(int RoundNumber, int StepNumber, bool PlayExcerpt, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
