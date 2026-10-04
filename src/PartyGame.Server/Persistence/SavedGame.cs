using PartyGame.Engine;

namespace PartyGame.Server.Persistence;

/// <summary>
/// The content of the file the game is saved to after each change, to resume it after a crash (ADR 0005).
/// </summary>
/// <param name="FormatVersion">The version of this format: a server does not resume a game saved in a format it does not know.</param>
/// <param name="SavedAt">Server time of the save, from which a resumed game tells how long it was interrupted.</param>
/// <param name="Game">The complete state of the game, secrets included. Never the code of the game master.</param>
internal sealed record SavedGame(int FormatVersion, DateTimeOffset SavedAt, GameState Game)
{
    /// <summary>The version of the format this server writes.</summary>
    public const int CurrentFormatVersion = 1;
}
