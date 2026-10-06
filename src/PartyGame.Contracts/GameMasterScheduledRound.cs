namespace PartyGame.Contracts;

/// <summary>
/// A round of the programme, as the game master console lists it: never its questions nor its answers.
/// </summary>
/// <param name="RoundIndex">Position of its activity in the pack, from 0, which the console names to reorder the programme.</param>
/// <param name="Title">The title of the round.</param>
/// <param name="Mode">The game mode that plays it, as the <c>type</c> of the activity names it, such as <c>quiz</c>.</param>
/// <param name="Status">Where the round stands in the programme.</param>
public sealed record GameMasterScheduledRound(int RoundIndex, string Title, string Mode, ScheduledRoundStatus Status);
