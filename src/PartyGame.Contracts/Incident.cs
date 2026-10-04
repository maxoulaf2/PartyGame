namespace PartyGame.Contracts;

/// <summary>
/// An incident of the server, as the game master console lists it. The occurrences of a same code in a same round, for a
/// same role and a same step, make one incident. It carries no text meant for a human, nor any trace of an exception: the
/// console translates the code.
/// </summary>
/// <param name="Id">Identifies the incident while its occurrences are counted, so that the console tells what it read.</param>
/// <param name="Code">What went wrong.</param>
/// <param name="Round">The round in progress when it happened, or <see langword="null"/> outside a round.</param>
/// <param name="Role">The role whose snapshot could not be projected, for <see cref="IncidentCode.ProjectionFailed"/> only.</param>
/// <param name="Step">
/// The step of <paramref name="Round"/> concerned, from 1, such as the number of a quiz question, for
/// <see cref="IncidentCode.DisplayMediaFailed"/> only, and when the game mode can tell.
/// </param>
/// <param name="Count">How many times it happened, at least 1.</param>
/// <param name="LastOccurredAt">When it last happened, in milliseconds since the Unix epoch on the clock of the server.</param>
public sealed record Incident(int Id, IncidentCode Code, RoundInfo? Round, Role? Role, int? Step, int Count, long LastOccurredAt);
