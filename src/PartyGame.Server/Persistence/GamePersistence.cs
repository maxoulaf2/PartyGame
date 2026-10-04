using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Modes;
using PartyGame.Server.Games;
using PartyGame.Server.Incidents;

namespace PartyGame.Server.Persistence;

/// <summary>
/// Saves the game to <see cref="FileName"/> after each change, so that a crash of the server loses nothing. The loop never
/// waits for the disk: it hands each new state over, and a single writer saves the last one handed, so that a burst of
/// changes makes a few writes, and an older state never overwrites a newer one. Each write goes to a temporary file first,
/// then replaces the previous save: a crash in the middle leaves the previous save intact.
/// </summary>
/// <remarks>
/// A failed write never stops the game: the game master is told once per series of failures, until a write succeeds.
/// Registered as a hosted service before the loop, it stops after it, and saves its last state before the process exits.
/// </remarks>
internal sealed class GamePersistence : BackgroundService, IGameStateListener
{
    public const string FileName = "current-game.json";

    public const string TemporaryFileName = FileName + ".tmp";

    // The last state not saved yet, the one before dropped: only the latest matters.
    private readonly Channel<GameState> _pending = Channel.CreateBounded<GameState>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = true });

    private readonly string _file;
    private readonly string _temporaryFile;
    private readonly JsonSerializerOptions _json;
    private readonly TimeProvider _timeProvider;
    private readonly IIncidentReporter _incidents;
    private readonly ILogger<GamePersistence> _logger;
    private bool _failing;

    public GamePersistence(
        IOptions<PersistenceOptions> options,
        GameModes modes,
        TimeProvider timeProvider,
        IIncidentReporter incidents,
        ILogger<GamePersistence> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        var directory = options.Value.FullDirectory;
        _file = Path.Combine(directory, FileName);
        _temporaryFile = Path.Combine(directory, TemporaryFileName);
        _json = GameStateJson.CreateOptions(modes);
        _timeProvider = timeProvider;
        _incidents = incidents;
        _logger = logger;
    }

    /// <summary>
    /// Makes sure the game can be saved before the server listens: creates the folder, deletes the temporary file of a
    /// save a crash interrupted, and checks that a file can be written there.
    /// </summary>
    /// <exception cref="DataDirectoryException">The folder cannot be created or written to.</exception>
    public static void PrepareDirectory(string directory, ILogger logger)
    {
        try
        {
            Directory.CreateDirectory(directory);

            var temporaryFile = Path.Combine(directory, TemporaryFileName);
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
                logger.TemporaryFileDeleted(temporaryFile);
            }

            var probe = Path.Combine(directory, $"{FileName}.probe");
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw new DataDirectoryException(directory, ex);
        }
    }

    public ValueTask OnStateChangedAsync(GameState state, CancellationToken cancellationToken)
    {
        // Never full: the oldest state waiting is dropped instead.
        _pending.Writer.TryWrite(state);
        return ValueTask.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await _pending.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (_pending.Reader.TryRead(out var state))
                {
                    await SaveAsync(state).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown: the loop stopped first, its last state may still be waiting.
        }

        if (_pending.Reader.TryRead(out var last))
        {
            await SaveAsync(last).ConfigureAwait(false);
        }
    }

    // Not cancellable: an interrupted write would only leave the previous save, and the shutdown waits for the last one.
    private async ValueTask SaveAsync(GameState state)
    {
        try
        {
            var stream = new FileStream(_temporaryFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await using (stream.ConfigureAwait(false))
            {
                var saved = new SavedGame(SavedGame.CurrentFormatVersion, _timeProvider.GetUtcNow(), state);
                await JsonSerializer.SerializeAsync(stream, saved, _json, CancellationToken.None).ConfigureAwait(false);

                // On the disk before it replaces the previous save, so that a power cut cannot leave an empty file instead.
                stream.Flush(flushToDisk: true);
            }

            File.Move(_temporaryFile, _file, overwrite: true);
        }
        catch (Exception ex)
        {
            // Whatever the cause, a bug included, the game goes on: only its resumption is at stake.
            _logger.GameNotSaved(ex, _file);
            if (!_failing)
            {
                _failing = true;
                await _incidents.ReportIncidentAsync(IncidentCode.PersistenceFailed, round: null, step: null, CancellationToken.None).ConfigureAwait(false);
            }

            return;
        }

        if (_failing)
        {
            _failing = false;
            _logger.GameSavedAgain(_file);
            await _incidents.ResolveAsync(IncidentCode.PersistenceFailed, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
