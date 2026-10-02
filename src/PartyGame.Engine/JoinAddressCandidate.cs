namespace PartyGame.Engine;

/// <summary>
/// An address the game master may advertise to phones. The server detects the candidates at startup; the engine only
/// checks that a choice is one of them.
/// </summary>
/// <param name="Address">The IPv4 address.</param>
/// <param name="InterfaceName">
/// Name of the network interface that holds the address, or <see langword="null"/> for an address imposed by the
/// configuration that no detected interface holds.
/// </param>
public sealed record JoinAddressCandidate(string Address, string? InterfaceName);
