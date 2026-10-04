namespace PartyGame.Server.Persistence;

/// <summary>
/// The folder the game is saved to cannot be used: the server stops before listening, rather than run a game it could not
/// resume.
/// </summary>
internal sealed class DataDirectoryException(string directory, Exception innerException)
    : Exception($"Data directory {directory} cannot be used", innerException)
{
    public string Directory { get; } = directory;
}
