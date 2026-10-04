using System.Net;

namespace PartyGame.Server.Network;

/// <summary>An IPv4 address of a network interface, with the length of the prefix of its network.</summary>
internal sealed record InterfaceAddress(IPAddress Address, int PrefixLength);
