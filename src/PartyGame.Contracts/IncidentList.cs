using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// Every incident the server still keeps, for the game master alone: sent whole on each new incident and when a console is
/// accepted, never as a difference. Incidents are not part of the game: they change no snapshot.
/// </summary>
/// <param name="Version">
/// The number of changes of the list since the server started, one more with each occurrence and each incident forgotten
/// once its cause is over: the console ignores an older list.
/// </param>
/// <param name="Incidents">The incidents, the one that happened last first.</param>
/// <param name="FailingRounds">
/// The rounds whose inputs failed often enough, or that the TV screen could not show, for the game master to be offered to
/// skip them, in the order they reached that point. Over or not: the console offers to skip the one in progress only.
/// </param>
public sealed record IncidentList(long Version, ImmutableArray<Incident> Incidents, ImmutableArray<RoundId> FailingRounds);
