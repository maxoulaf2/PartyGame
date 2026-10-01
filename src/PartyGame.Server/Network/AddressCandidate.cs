using System.Net;

namespace PartyGame.Server.Network;

/// <summary>A private IPv4 address of a physical interface, at which phones may reach the server.</summary>
internal sealed record AddressCandidate(IPAddress Address, string InterfaceName, bool HasGateway);
