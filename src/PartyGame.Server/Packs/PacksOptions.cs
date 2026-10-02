namespace PartyGame.Server.Packs;

internal sealed class PacksOptions
{
    public const string SectionName = "Packs";

    public const string DefaultDirectory = "packs";

    public const string DirectorySetting = $"{SectionName}:{nameof(Directory)}";

    // Folder holding one subfolder per pack. A relative path is relative to the folder of the application, not to the
    // current directory, so that the server finds its packs however it is launched.
    public string Directory { get; init; } = DefaultDirectory;

    public string FullDirectory => Path.GetFullPath(Directory, AppContext.BaseDirectory);
}
