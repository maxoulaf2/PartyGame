namespace PartyGame.Contracts;

/// <summary>
/// The game master shows or hides the QR code on the TV screen once the game has started, for a late arrival to join.
/// </summary>
/// <param name="Shown">
/// Whether the QR code is shown. A value rather than a toggle: a request sent twice (double tap, second console, request
/// sent again after a reconnection) leaves the TV screen as the game master asked.
/// </param>
public sealed record ShowJoinCodeRequest(bool Shown);
