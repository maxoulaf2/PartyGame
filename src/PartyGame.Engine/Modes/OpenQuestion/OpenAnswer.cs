namespace PartyGame.Engine.Modes.OpenQuestion;

/// <summary>
/// The answer of a player to the open question in progress: only the first one counts.
/// </summary>
/// <param name="Text">The answer as typed, kept for the screens: it is normalized only to be compared.</param>
/// <param name="ReceivedAt">
/// Server time at which the hub received the answer, for the speed bonus: what counts is when the answer arrived, not
/// when the loop handled it.
/// </param>
public sealed record OpenAnswer(string Text, DateTimeOffset ReceivedAt);
