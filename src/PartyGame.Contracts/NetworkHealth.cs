using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// How the devices reach the server, for the game master alone. These measures change often and are not part of the game:
/// they change no snapshot, and are sent whole every few seconds and when a console is accepted.
/// </summary>
/// <param name="Diagnostics">The last network diagnostics run on the diagnostic page, the most recent first.</param>
/// <param name="Connections">The players and the TV screen that connected since the server started.</param>
public sealed record NetworkHealth(ImmutableArray<NetworkDiagnostic> Diagnostics, ImmutableArray<ConnectionQuality> Connections);
