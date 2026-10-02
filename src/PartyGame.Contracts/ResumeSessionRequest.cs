namespace PartyGame.Contracts;

/// <summary>
/// A phone that joined before presents its token on a new connection, after a reload, a sleep or a lost network, to be
/// recognized as the same player without registering again.
/// </summary>
/// <param name="Token">The token received in the <see cref="JoinResult"/> of the registration.</param>
public sealed record ResumeSessionRequest(string Token);
