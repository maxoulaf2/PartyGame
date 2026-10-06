namespace PartyGame.Server.Persistence;

internal sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public const string DefaultDirectory = "data";

    public const string DirectorySetting = $"{SectionName}:{nameof(Directory)}";

    // Folder of the saved game. A relative path is relative to the folder of the application, as for the packs, so that a
    // restarted server finds its game however it is launched.
    public string Directory { get; init; } = DefaultDirectory;

    public string FullDirectory => Path.GetFullPath(Directory, AppContext.BaseDirectory);

    // Where the zip packs are extracted: kept across restarts, so that an unchanged zip is not extracted again.
    public string FullPackCacheDirectory => Path.Combine(FullDirectory, "packs-cache");
}
