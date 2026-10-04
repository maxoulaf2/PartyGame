namespace PartyGame.Contracts;

/// <summary>
/// The diagnostic page tells the server the outcome of its test, for the game master console to list it. Any connection
/// may send one, identified or not: the page does not register.
/// </summary>
/// <param name="Verdict">The verdict the page showed.</param>
/// <param name="RoundTripMedian">
/// The median round trip to the hub, in milliseconds, or <see langword="null"/> when no round trip came back.
/// </param>
public sealed record NetworkDiagnosticReport(DiagnosticVerdict Verdict, int? RoundTripMedian);
