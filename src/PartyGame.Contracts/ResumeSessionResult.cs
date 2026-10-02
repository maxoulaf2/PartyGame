namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub to a <see cref="ResumeSessionRequest"/>. Once the session is resumed, the connection gets the
/// snapshots of the player, starting with the current one.
/// </summary>
/// <param name="Refusal">Why the session was not resumed, or <see langword="null"/> when it was.</param>
/// <param name="PlayerId">The player the token identifies, or <see langword="null"/> when refused.</param>
public sealed record ResumeSessionResult(ResumeSessionRefusal? Refusal, PlayerId? PlayerId);
