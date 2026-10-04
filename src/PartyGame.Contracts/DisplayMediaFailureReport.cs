namespace PartyGame.Contracts;

/// <summary>
/// The TV screen tells the server that it could not load a media file of the pack, such as the image of a question: it
/// goes on without it, and the server tells the game master which round and which step of it are concerned.
/// </summary>
/// <param name="MediaId">
/// The identifier of the media file, as its URL gives it after <c>/media/</c>: the TV screen knows nothing else of it.
/// </param>
public sealed record DisplayMediaFailureReport(string MediaId);
