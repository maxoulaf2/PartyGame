using System.Globalization;
using PartyGame.Content;
using PartyGame.Engine.Modes;
using PartyGame.Server.Games;
using PartyGame.Server.Logging;

namespace PartyGame.Server.Packs;

/// <summary>
/// The <c>validate</c> command: checks a pack, or every pack of a folder, with the loading of the server and the checks of
/// its game modes, then exits without listening on the network. It writes for the pack author, in French.
/// </summary>
internal static class PackValidationCommand
{
    public const string Name = "validate";

    public const int Valid = 0;

    public const int Invalid = 1;

    public const int NotFound = 2;

    public const int Failed = 3;

    public static int Run(IReadOnlyList<string> arguments, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);

        if (arguments.Count != 1)
        {
            output.WriteLine("Usage : PartyGame.Server validate <dossier d'un pack, ou dossier de packs>");
            return NotFound;
        }

        try
        {
            return Validate(Path.GetFullPath(arguments[0]), output);
        }
        catch (Exception ex)
        {
            Log(logger => logger.Error(ex, "Pack validation of {Path} failed", arguments[0]));
            output.WriteLine($"Erreur inattendue pendant la vérification : {ex.Message} (détail dans le journal du serveur).");
            return Failed;
        }
    }

    private static int Validate(string path, TextWriter output)
    {
        // A pack.json given directly stands for its pack.
        if (File.Exists(path) && Path.GetFileName(path) == Contracts.Packs.PackDescriptor.FileName)
        {
            path = Path.GetDirectoryName(path)!;
        }

        if (!Directory.Exists(path))
        {
            output.WriteLine(File.Exists(path)
                ? $"{path} n'est ni un pack ni un dossier de packs : un pack est un dossier qui contient un fichier pack.json."
                : $"{path} est introuvable.");
            return NotFound;
        }

        var loader = new PackLoader(CreateModes().Validate);
        if (PackLoader.HasDescriptor(path))
        {
            return Report(loader.Load(path), output) ? Valid : Invalid;
        }

        var library = loader.LoadAll(path);
        if (library.Packs.IsEmpty)
        {
            output.WriteLine($"{path} n'est ni un pack ni un dossier de packs : aucun de ses sous-dossiers ne contient de fichier pack.json.");
            return NotFound;
        }

        var valid = library.Packs.Count(pack => Report(pack, output));
        var invalid = library.Packs.Length - valid;
        output.WriteLine($"{Plural(valid, "pack valide", "packs valides")}, {Plural(invalid, "invalide", "invalides")}");
        return invalid == 0 ? Valid : Invalid;
    }

    // Writes what the author needs to know of a pack, and tells whether it is valid.
    private static bool Report(LoadedPack pack, TextWriter output)
    {
        if (pack.Failure is { } failure)
        {
            Log(logger => logger.Error(failure, "Pack {PackId} failed to load from {Folder}", pack.Id, pack.Folder));
        }

        output.WriteLine($"Pack {(pack.Title is null ? pack.Id : $"« {pack.Title} »")} ({pack.Folder})");
        if (pack.IsValid)
        {
            foreach (var (round, index) in pack.Descriptor.Rounds.Select((round, index) => (round, index)))
            {
                output.WriteLine($"  Manche {index + 1} : {round.Title} ({GameModes.TypeOf(round)})");
            }

            output.WriteLine("  Pack valide");
        }
        else
        {
            foreach (var problem in pack.Problems)
            {
                output.WriteLine($"  {problem.File}, {problem.Path} : {PackProblemMessages.Describe(problem)} [{problem.Code}]");
            }

            output.WriteLine($"  Pack invalide : {Plural(pack.Problems.Length, "problème", "problèmes")}");
        }

        output.WriteLine();
        return pack.IsValid;
    }

    // The same modes as the server, registered the same way: the command cannot diverge from the loading of the server.
    private static GameModes CreateModes()
    {
        using var services = new ServiceCollection().AddGameModes().BuildServiceProvider();
        return services.GetRequiredService<GameModes>();
    }

    // Writes to the log files of the server only: the console keeps a short message for the author.
    private static void Log(Action<Serilog.ILogger> write)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();
        using var logger = ServerLogging.CreateLogger(configuration, console: false);
        write(logger);
    }

    private static string Plural(int count, string one, string many) =>
        string.Create(CultureInfo.InvariantCulture, $"{count} {(count > 1 ? many : one)}");
}
