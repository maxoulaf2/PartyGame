namespace PartyGame.Contracts;

/// <summary>
/// The TV screen tells the server whether its browser lets it play sound, so that the game master knows before the first
/// round whether to click « Démarrer » on it. Sent when the screen identifies, and again once its audio is unlocked.
/// </summary>
/// <param name="Unlocked">Whether the browser of the TV screen lets it play sound without another click.</param>
public sealed record DisplayAudioReport(bool Unlocked);
