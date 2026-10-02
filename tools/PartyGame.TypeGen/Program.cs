using PartyGame.TypeGen;

const string Usage = "Usage: PartyGame.TypeGen <output-directory> [--schema <pack-schema-file>] [--verify]";
const string Command = "npm run generate:contracts";

var verify = false;
string? schemaPath = null;
var positional = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--verify":
            verify = true;
            break;
        case "--schema" when i + 1 < args.Length:
            schemaPath = args[++i];
            break;
        default:
            positional.Add(args[i]);
            break;
    }
}

if (positional is not [var outputDirectory] || outputDirectory.StartsWith("--", StringComparison.Ordinal))
{
    Console.Error.WriteLine(Usage);
    return 2;
}

try
{
    var files = ContractsGenerator.Generate(ContractsGenerator.ContractsAssembly);
    var schema = schemaPath is null ? null : PackSchemaGenerator.Generate();

    if (verify)
    {
        var differences = OutputDirectory.FindDifferences(outputDirectory, files)
            .Select(difference => $"{outputDirectory}: {difference}")
            .ToList();
        if (schemaPath is not null && OutputFile.FindDifference(schemaPath, schema!, schemaPath) is { } schemaDifference)
        {
            differences.Add(schemaDifference);
        }

        if (differences.Count > 0)
        {
            Console.Error.WriteLine("Generated files are out of date:");
            foreach (var difference in differences)
            {
                Console.Error.WriteLine($"  - {difference}");
            }

            Console.Error.WriteLine($"Run `{Command}` from client/ to update them.");
            return 1;
        }

        Console.WriteLine("Generated files are up to date.");
        return 0;
    }

    OutputDirectory.Write(outputDirectory, files);
    Console.WriteLine($"Generated {files.Length} TypeScript files in {outputDirectory}.");

    if (schemaPath is not null)
    {
        OutputFile.Write(schemaPath, schema!);
        Console.WriteLine($"Generated the pack schema in {schemaPath}.");
    }

    return 0;
}
catch (TypeGenException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}
