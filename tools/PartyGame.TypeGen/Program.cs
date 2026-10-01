using PartyGame.TypeGen;

const string Usage = "Usage: PartyGame.TypeGen <output-directory> [--verify]";
const string Command = "npm run generate:contracts";

var verify = args.Contains("--verify");
var positional = args.Where(a => a != "--verify").ToList();
if (positional is not [var outputDirectory])
{
    Console.Error.WriteLine(Usage);
    return 2;
}

try
{
    var files = ContractsGenerator.Generate(ContractsGenerator.ContractsAssembly);

    if (verify)
    {
        var differences = OutputDirectory.FindDifferences(outputDirectory, files);
        if (differences.Count > 0)
        {
            Console.Error.WriteLine($"Generated TypeScript contracts in {outputDirectory} are out of date:");
            foreach (var difference in differences)
            {
                Console.Error.WriteLine($"  - {difference}");
            }

            Console.Error.WriteLine($"Run `{Command}` from client/ to update them.");
            return 1;
        }

        Console.WriteLine("Generated TypeScript contracts are up to date.");
        return 0;
    }

    OutputDirectory.Write(outputDirectory, files);
    Console.WriteLine($"Generated {files.Length} TypeScript files in {outputDirectory}.");
    return 0;
}
catch (TypeGenException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}
