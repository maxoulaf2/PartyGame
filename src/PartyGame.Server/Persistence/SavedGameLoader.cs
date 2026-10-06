using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Modes;
using PartyGame.Server.Incidents;

namespace PartyGame.Server.Persistence;

/// <summary>
/// Reads, once at startup, the game the previous run of the server saved, for the game master to resume it. A file that
/// cannot be read, or written in a format this server does not know, is set aside under another name rather than deleted,
/// and the game master is told that the players will register again.
/// </summary>
internal sealed class SavedGameLoader(
    IOptions<PersistenceOptions> options,
    GameModes modes,
    TimeProvider timeProvider,
    IncidentJournal incidents,
    ILogger<SavedGameLoader> logger)
{
    // Strict: a property missing or null where the state allows none would only fail later, during the game.
    private readonly JsonSerializerOptions _json = new(GameStateJson.CreateOptions(modes))
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    /// <summary>
    /// The game saved by the previous run, to offer to the game master; <see langword="null"/> when there is none, when it
    /// has no player, or when it cannot be read.
    /// </summary>
    public SavedGame? Load()
    {
        ArgumentNullException.ThrowIfNull(options);

        var file = Path.Combine(options.Value.FullDirectory, GamePersistence.FileName);
        if (!File.Exists(file))
        {
            return null;
        }

        SavedGame? saved;
        string? problem;
        try
        {
            saved = JsonSerializer.Deserialize<SavedGame>(File.ReadAllBytes(file), _json);
            problem = saved switch
            {
                null => "no game in the file",
                { FormatVersion: not SavedGame.CurrentFormatVersion } => $"format version {saved.FormatVersion} unknown",
                _ => null,
            };
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            saved = null;
            problem = ex.Message;
        }

        if (problem is not null)
        {
            SetAside(file, problem);
            return null;
        }

        if (saved!.Game.Players.IsEmpty)
        {
            // Nothing worth resuming: the new game replaces it at its first save.
            logger.SavedGameWithoutPlayer(file);
            return null;
        }

        logger.SavedGameFound(file, saved.Game.Players.Length, saved.SavedAt);
        return WithSchedule(saved);
    }

    /// <summary>
    /// The game saved with its programme. A game saved before the programme existed has none: its rounds follow the order
    /// of the pack. With more than one round, a game started always has some round past, to come or withdrawn.
    /// </summary>
    private static SavedGame WithSchedule(SavedGame saved) =>
        saved.Game is { CurrentRound: { } round, Rounds.Length: > 1, Schedule: { Past.IsEmpty: true, Upcoming.IsEmpty: true, Withdrawn.IsEmpty: true } }
            ? saved with { Game = saved.Game with { Schedule = RoundSchedule.InPackOrder(round.Index, saved.Game.Rounds.Length) } }
            : saved;

    private void SetAside(string file, string problem)
    {
        var name = string.Create(
            CultureInfo.InvariantCulture,
            $"{Path.GetFileNameWithoutExtension(GamePersistence.FileName)}.unreadable-{timeProvider.GetLocalNow():yyyyMMdd-HHmmss}.json");
        var aside = Path.Combine(Path.GetDirectoryName(file)!, name);
        try
        {
            File.Move(file, aside, overwrite: true);
            logger.SavedGameUnreadable(file, problem, aside);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The new game replaces it at its first save: the file is lost, not the evening.
            logger.SavedGameNotSetAside(ex, file, problem);
        }

        // No console is connected yet: the journal hands the incident to the first one.
        incidents.Record(IncidentCode.SavedGameUnreadable, round: null);
    }
}
