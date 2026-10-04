using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Server.Persistence;
using PartyGame.Server.Tests.Games;

namespace PartyGame.Server.Tests.Persistence;

public sealed class GamePersistenceTests : IAsyncDisposable
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private readonly TempDirectory _data = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 4, 21, 0, 0, TimeSpan.Zero));
    private readonly RecordingIncidentReporter _incidents = new();
    private readonly RecordingLogger<GamePersistence> _logger = new();
    private readonly GamePersistence _persistence;

    public GamePersistenceTests()
    {
        GamePersistence.PrepareDirectory(_data.Path, _logger);
        _persistence = new GamePersistence(
            Options.Create(new PersistenceOptions { Directory = _data.Path }),
            new GameModes([new QuizMode()]),
            _time,
            _incidents,
            _logger);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private string SaveFile => Path.Combine(_data.Path, GamePersistence.FileName);

    private string TemporaryFile => Path.Combine(_data.Path, GamePersistence.TemporaryFileName);

    public async ValueTask DisposeAsync()
    {
        await _persistence.StopAsync(CancellationToken.None);
        _persistence.Dispose();
        _data.Dispose();
    }

    [Fact]
    public async Task OnStateChanged_StateChanged_SavesItWithTheVersionOfTheFormatAndTheTimeOfTheSave()
    {
        // Given
        await _persistence.StartAsync(Ct);

        // When
        await _persistence.OnStateChangedAsync(State(version: 2), Ct);

        // Then
        await EventuallyAsync(() => SavedVersion() == 2);
        var saved = ReadSave();
        Assert.Equal(SavedGame.CurrentFormatVersion, saved["formatVersion"]!.GetValue<int>());
        Assert.Equal(_time.GetUtcNow(), saved["savedAt"]!.GetValue<DateTimeOffset>());
        Assert.Equal("192.168.1.42", saved["game"]!["joinAddress"]!.GetValue<string>());
        Assert.False(File.Exists(TemporaryFile));
    }

    [Fact]
    public async Task OnStateChanged_Burst_NeverWaitsForTheDiskAndSavesTheLatestState()
    {
        // Given
        await _persistence.StartAsync(Ct);

        // When: far more changes than the disk can follow one by one
        for (var version = 2; version <= 500; version++)
        {
            // Then: the loop goes on at once
            var handed = _persistence.OnStateChangedAsync(State(version), Ct);
            Assert.True(handed.IsCompletedSuccessfully);
            await handed;
        }

        // Then: an older state never overwrites the latest
        await EventuallyAsync(() => SavedVersion() == 500);
        await _persistence.StopAsync(Ct);
        Assert.Equal(500, SavedVersion());
        Assert.Empty(_incidents.Detached);
    }

    [Fact]
    public async Task StopAsync_StateJustChanged_SavesItBeforeStopping()
    {
        // Given
        await _persistence.StartAsync(Ct);
        await _persistence.OnStateChangedAsync(State(version: 2), Ct);
        await EventuallyAsync(() => SavedVersion() == 2);

        // When
        await _persistence.OnStateChangedAsync(State(version: 3), Ct);
        await _persistence.StopAsync(Ct);

        // Then
        Assert.Equal(3, SavedVersion());
    }

    [Fact]
    public async Task OnStateChanged_WriteFailsThenSucceeds_ReportsOnceForTheSeriesThenForgetsTheIncident()
    {
        // Given: the folder disappears during the game
        await _persistence.StartAsync(Ct);
        Directory.Delete(_data.Path, recursive: true);

        // When
        await _persistence.OnStateChangedAsync(State(version: 2), Ct);
        await EventuallyAsync(() => Errors() == 1);
        await _persistence.OnStateChangedAsync(State(version: 3), Ct);
        await EventuallyAsync(() => Errors() == 2);

        // Then: each failure is logged, the game master told once
        Assert.Equal([IncidentCode.PersistenceFailed], _incidents.Detached);
        Assert.Empty(_incidents.Resolved);

        // When: the folder is back
        Directory.CreateDirectory(_data.Path);
        await _persistence.OnStateChangedAsync(State(version: 4), Ct);

        // Then
        await EventuallyAsync(() => _incidents.Resolved.Count == 1);
        Assert.Equal([IncidentCode.PersistenceFailed], _incidents.Resolved);
        Assert.Equal(4, SavedVersion());
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("saved again", StringComparison.Ordinal));

        // When: it fails again, a new series begins
        Directory.Delete(_data.Path, recursive: true);
        await _persistence.OnStateChangedAsync(State(version: 5), Ct);

        // Then
        await EventuallyAsync(() => _incidents.Detached.Count == 2);
    }

    [Fact]
    public void PrepareDirectory_Missing_CreatesIt()
    {
        var directory = Path.Combine(_data.Path, "nouveau", "data");

        GamePersistence.PrepareDirectory(directory, _logger);

        Assert.True(Directory.Exists(directory));
        Assert.Empty(Directory.GetFiles(directory));
    }

    [Fact]
    public void PrepareDirectory_TemporaryFileOfAnInterruptedSave_DeletesItAndKeepsThePreviousSave()
    {
        // Given: the server stopped in the middle of a write
        File.WriteAllText(SaveFile, "{\"previous\":true}");
        File.WriteAllText(TemporaryFile, "{\"gam");

        // When
        GamePersistence.PrepareDirectory(_data.Path, _logger);

        // Then
        Assert.False(File.Exists(TemporaryFile));
        Assert.Equal("{\"previous\":true}", File.ReadAllText(SaveFile));
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains(TemporaryFile, StringComparison.Ordinal));
    }

    [Fact]
    public void PrepareDirectory_CannotBeCreated_ThrowsNamingIt()
    {
        // Given: a file stands where the folder should be
        var file = Path.Combine(_data.Path, "fichier");
        File.WriteAllText(file, string.Empty);
        var directory = Path.Combine(file, "data");

        // When
        var exception = Assert.Throws<DataDirectoryException>(() => GamePersistence.PrepareDirectory(directory, _logger));

        // Then
        Assert.Equal(directory, exception.Directory);
        Assert.IsAssignableFrom<IOException>(exception.InnerException);
    }

    private static GameState State(long version)
    {
        var state = GameState.Create(new GameId(Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff")), "192.168.1.42", [], new PackCatalog("packs", []));
        return state with { Version = version };
    }

    private static async Task EventuallyAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + _timeout;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The condition was not met in time.");
            await Task.Delay(10, Ct);
        }
    }

    private int Errors() => _logger.Entries.Count(e => e.Level == LogLevel.Error);

    private long? SavedVersion() => File.Exists(SaveFile) ? ReadSave()["game"]!["version"]!.GetValue<long>() : null;

    // Shared for deletion too: the writer may replace the file while the test reads it.
    private JsonNode ReadSave()
    {
        using var stream = new FileStream(SaveFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return JsonNode.Parse(stream)!;
    }
}
