using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// Every incident the server still keeps, for the game master alone: sent whole on each new incident and when a console is
/// accepted, never as a difference. Incidents are not part of the game: they change no snapshot.
/// </summary>
/// <param name="Version">
/// The number of occurrences since the server started, one more with each: the console ignores an older list, and counts
/// the occurrences since it last marked them as read.
/// </param>
/// <param name="Incidents">The incidents, the one that happened last first.</param>
public sealed record IncidentList(long Version, ImmutableArray<Incident> Incidents);
