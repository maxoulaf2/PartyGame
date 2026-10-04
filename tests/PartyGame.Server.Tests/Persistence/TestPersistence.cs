using Microsoft.Extensions.Options;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Server.Persistence;
using PartyGame.Server.Tests.Games;

namespace PartyGame.Server.Tests.Persistence;

internal static class TestPersistence
{
    /// <summary>
    /// A persistence of the game to <paramref name="directory"/>, not started: enough to execute its effects.
    /// </summary>
    public static GamePersistence In(string directory, TimeProvider? time = null) =>
        new(
            Options.Create(new PersistenceOptions { Directory = directory }),
            new GameModes([new QuizMode()]),
            time ?? TimeProvider.System,
            new RecordingIncidentReporter(),
            new RecordingLogger<GamePersistence>());
}
