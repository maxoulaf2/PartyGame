using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The server checked again, at the request of the game master, the media files of the game it found saved: the files are
/// read outside the loop, and only their result reaches the engine.
/// </summary>
/// <param name="SavedGameId">The game whose media files were checked.</param>
/// <param name="MissingMedia">Its media files missing from the disk now.</param>
public sealed record SavedGameMediaChecked(GameId SavedGameId, ImmutableArray<MediaPath> MissingMedia) : GameInput;
