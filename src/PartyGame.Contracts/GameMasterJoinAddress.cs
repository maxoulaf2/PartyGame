namespace PartyGame.Contracts;

/// <summary>
/// An address the game master may advertise to phones, encoded in the QR code of the TV screen.
/// </summary>
/// <param name="Address">The IPv4 address, as a choice designates it.</param>
/// <param name="InterfaceName">
/// Name of the network interface that holds the address, to tell the networks apart, or <see langword="null"/> for an
/// address imposed by the configuration that no detected interface holds.
/// </param>
public sealed record GameMasterJoinAddress(string Address, string? InterfaceName);
