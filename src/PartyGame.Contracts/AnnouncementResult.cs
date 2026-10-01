namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub to an <see cref="Announcement"/>.
/// </summary>
/// <param name="Refusal">Why the announcement was refused, or <see langword="null"/> when the connection now has the role it claimed.</param>
public sealed record AnnouncementResult(AnnouncementRefusal? Refusal);
