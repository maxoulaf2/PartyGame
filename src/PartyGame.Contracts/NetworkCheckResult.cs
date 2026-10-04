namespace PartyGame.Contracts;

/// <summary>
/// What the server sees of the connection of the diagnostic page: the page cannot tell either by itself.
/// </summary>
/// <param name="Transport">How the page reaches the hub.</param>
/// <param name="SameSubnet">
/// Whether the address of the device belongs to the network of the address advertised to phones, or
/// <see langword="null"/> when the server cannot tell, such as when it advertises none. A device on another network, such
/// as a guest Wi-Fi, may reach the server more slowly, or not at all.
/// </param>
public sealed record NetworkCheckResult(ConnectionTransport Transport, bool? SameSubnet);
