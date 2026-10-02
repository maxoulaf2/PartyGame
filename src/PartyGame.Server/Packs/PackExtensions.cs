using PartyGame.Content;
using PartyGame.Server.Games;

namespace PartyGame.Server.Packs;

internal static class PackExtensions
{
    /// <summary>
    /// Registers the packs of the pack directory, loaded and checked at startup and on each reload, with the consistency
    /// checks of the registered game modes.
    /// </summary>
    public static WebApplicationBuilder AddPacks(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<PacksOptions>()
            .BindConfiguration(PacksOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Directory), $"{PacksOptions.DirectorySetting} must not be empty")
            .ValidateOnStart();

        builder.Services.AddGameModes();
        builder.Services.AddSingleton<PackLibraryLoader>();
        builder.Services.AddSingleton<PackReloader>();

        // The packs as loaded at startup: the banner lists them, and the game starts with them.
        builder.Services.AddSingleton(services => services.GetRequiredService<PackLibraryLoader>().Load());

        return builder;
    }

    /// <summary>
    /// Loads the packs now, before the server listens: the hub never accepts a connection while they are not all checked.
    /// </summary>
    public static PackLibrary LoadPacks(this WebApplication app) => app.Services.GetRequiredService<PackLibrary>();
}
