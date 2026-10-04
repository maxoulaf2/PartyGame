namespace PartyGame.Contracts;

/// <summary>
/// A network diagnostic a device ran, as the game master console lists it.
/// </summary>
/// <param name="Device">The kind of device, deduced from its browser.</param>
/// <param name="ReportedAt">When the server received it, in milliseconds since the Unix epoch on the clock of the server.</param>
/// <param name="Verdict">The verdict the device showed.</param>
/// <param name="RoundTripMedian">The median round trip, in milliseconds, or <see langword="null"/> when none came back.</param>
public sealed record NetworkDiagnostic(DeviceKind Device, long ReportedAt, DiagnosticVerdict Verdict, int? RoundTripMedian);
