using Microsoft.Extensions.Options;
using PartyGame.Content;
using PartyGame.Engine.Modes;
using PartyGame.Server.Games;

namespace PartyGame.Server.Packs;

internal static class PackExtensions
{
    /// <summary>
    /// Registers the packs of the pack directory, loaded and checked once, with the consistency checks of the registered
    /// game modes.
    /// </summary>
    public static WebApplicationBuilder AddPacks(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<PacksOptions>()
            .BindConfiguration(PacksOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Directory), $"{PacksOptions.DirectorySetting} must not be empty")
            .ValidateOnStart();

        builder.Services.AddGameModes();
        builder.Services.AddSingleton(LoadAll);

        return builder;
    }

    /// <summary>
    /// Loads the packs now, before the server listens: the hub never accepts a connection while they are not all checked.
    /// </summary>
    public static PackLibrary LoadPacks(this WebApplication app) => app.Services.GetRequiredService<PackLibrary>();

    private static PackLibrary LoadAll(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<PacksOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<PackLibrary>>();
        var modes = services.GetRequiredService<GameModes>();

        var library = new PackLoader(modes.Validate).LoadAll(options.FullDirectory);

        if (!library.DirectoryExists)
        {
            logger.PackDirectoryMissing(library.Directory, PacksOptions.DirectorySetting);
        }
        else if (library.Packs.IsEmpty)
        {
            logger.PackDirectoryEmpty(library.Directory);
        }

        foreach (var pack in library.Packs)
        {
            if (pack.Failure is not null)
            {
                logger.PackLoadFailed(pack.Failure, pack.Id, pack.Folder);
            }

            logger.PackLoaded(pack.Id, pack.Folder, pack.Title, pack.RoundCount, pack.Problems.Length);
            foreach (var problem in pack.Problems)
            {
                var parameters = string.Join(", ", problem.Parameters.OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                    .Select(parameter => $"{parameter.Key}={parameter.Value}"));
                logger.PackProblemFound(pack.Id, problem.Code, problem.File, problem.Path, parameters);
            }
        }

        return library;
    }
}
