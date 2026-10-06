using Microsoft.Extensions.Options;
using PartyGame.Content;
using PartyGame.Engine.Modes;
using PartyGame.Server.Persistence;

namespace PartyGame.Server.Packs;

/// <summary>
/// Loads and checks every pack of the pack directory, with the consistency checks of the registered game modes, and logs
/// what the operator needs to fix them: at startup, then on each reload asked by the game master.
/// </summary>
internal sealed class PackLibraryLoader(
    IOptions<PacksOptions> options,
    IOptions<PersistenceOptions> persistence,
    GameModes modes,
    ILogger<PackLibrary> logger)
{
    public PackLibrary Load()
    {
        var library = new PackLoader(modes.Validate).LoadAll(options.Value.FullDirectory, persistence.Value.FullPackCacheDirectory);

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
