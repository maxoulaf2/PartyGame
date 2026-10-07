using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.BlindTest;
using PartyGame.Engine.Modes.QuizBuzzer;
using PartyGame.Engine.Modes.OpenQuestion;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Engine.State;
using PartyGame.Engine.Tests.Modes.Quiz;
using PartyGame.Engine.Tests.Rounds;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests;

public sealed class GameStateJsonTests
{
    private static readonly JsonSerializerOptions _options =
        GameStateJson.CreateOptions(new GameModes([new FakeMode(), new QuizMode(), new BuzzerMode(), new BlindTestMode(), new OpenQuestionMode()]), FakeJson.Options);

    [Fact]
    public void RoundTrip_EveryStateOfEveryLeakSuite_IsRestoredUnchanged()
    {
        // Given: the leak suites cover every phase of every mode, so a new mode is covered by its own suite
        var suites = LeakSuites().ToList();
        Assert.Contains(suites, suite => suite.Name.Contains(nameof(QuizLeakTests), StringComparison.Ordinal));

        List<string> failures = [];
        foreach (var (suiteName, suite) in suites)
        {
            foreach (var (name, state) in suite.States())
            {
                // When
                var json = JsonSerializer.Serialize(state, _options);
                var restored = JsonSerializer.Deserialize<GameState>(json, _options)!;

                // Then: nothing is lost, down to what the projections show, but the preview of a pack, which is no game
                var context = $"{suiteName}, scenario \"{name}\"";
                if (JsonSerializer.Serialize(restored, _options) != json)
                {
                    failures.Add($"{context}: the restored state serializes differently");
                }

                foreach (var (viewer, projection) in suite.Project(state with { Preview = null }).All)
                {
                    if (!JsonNode.DeepEquals(projection, suite.Project(restored).For(viewer)))
                    {
                        failures.Add($"{context}, {viewer}: the restored state projects differently");
                    }
                }
            }
        }

        LeakAssert.Fail("States changed by their persistence", failures);
    }

    [Fact]
    public void Serialize_RoundState_NamesItsType()
    {
        var state = QuizGames.Started([QuizGames.Round(QuizGames.CapitalQuestion)], ["Zoé"]);

        var json = JsonSerializer.SerializeToNode(state, _options)!;

        Assert.Equal(nameof(QuizRound), json["currentRound"]!["state"]!["$type"]!.GetValue<string>());
    }

    // Every leak suite of a game state declared by this assembly, wherever its test class keeps it.
    private static IEnumerable<(string Name, ILeakSuite<GameState> Suite)> LeakSuites() =>
        typeof(GameStateJsonTests).Assembly.GetTypes()
            .SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            .Where(field => typeof(ILeakSuite<GameState>).IsAssignableFrom(field.FieldType))
            .Select(field => ($"{field.DeclaringType!.Name}.{field.Name}", (ILeakSuite<GameState>)field.GetValue(null)!));
}
