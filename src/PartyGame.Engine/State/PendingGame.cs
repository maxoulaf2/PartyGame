using System.Collections.Immutable;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.State;

/// <summary>
/// The game a restarted server found saved, kept aside while the game master decides whether to resume it or start a new
/// one. The server reads the file and checks the media files: the engine only gets their result.
/// </summary>
/// <param name="Game">The game as it was last saved, secrets included.</param>
/// <param name="SavedAt">Server time of its last save.</param>
/// <param name="MissingMedia">
/// The media files of its pack missing from the disk when last checked: the game cannot be resumed until they are back.
/// </param>
public sealed record PendingGame(GameState Game, DateTimeOffset SavedAt, ImmutableArray<MediaPath> MissingMedia);
