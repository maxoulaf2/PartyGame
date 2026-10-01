namespace PartyGame.Contracts;

/// <summary>
/// What the TV screen needs to invite phones to join: the address players reach the server at.
/// The client adds the port of the page it loaded, which is the one phones must use as well.
/// </summary>
/// <param name="Address">
/// The advertised IPv4 address, or <see langword="null"/> when the server found no local network address.
/// </param>
public sealed record JoinInfo(string? Address);
