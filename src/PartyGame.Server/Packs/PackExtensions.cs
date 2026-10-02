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
        builder.Services.AddSingleton<PackMediaFiles>();

        // The packs as loaded at startup: the banner lists them, and the game starts with them.
        builder.Services.AddSingleton(services => services.GetRequiredService<PackLibraryLoader>().Load());

        return builder;
    }

    /// <summary>
    /// Serves the media files of the pack of the game under <see cref="ServerPaths.Media"/>, by their identifier, with
    /// support for range requests so that an excerpt can start in the middle of a track.
    /// </summary>
    public static WebApplication MapPackMedia(this WebApplication app)
    {
        app.MapMethods(
            $"{ServerPaths.Media}/{{id}}",
            [HttpMethods.Get, HttpMethods.Head],
            (string id, PackMediaFiles media, HttpResponse response) => media.Serve(id, response));
        return app;
    }

    /// <summary>
    /// Loads the packs now, before the server listens: the hub never accepts a connection while they are not all checked.
    /// </summary>
    public static PackLibrary LoadPacks(this WebApplication app) => app.Services.GetRequiredService<PackLibrary>();
}
