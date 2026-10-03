using System.Collections.Immutable;
using PartyGame.Contracts;

namespace PartyGame.Server.Incidents;

/// <summary>
/// The incidents of the server since it started, for the game master. Kept apart from the game state: an incident changes
/// nothing of the game, and is forgotten on restart, the logs keeping their trace. The occurrences of a same code in a same
/// round, for a same role, make one incident; past <see cref="Capacity"/> incidents, the one that happened longest ago is
/// dropped.
/// </summary>
/// <remarks>
/// Fed by the loop only, but read by the hub when a console is accepted: the lock guards the list against that read.
/// </remarks>
internal sealed class IncidentJournal(TimeProvider timeProvider)
{
    /// <summary>The most incidents kept: enough for a whole evening, small enough to send whole each time.</summary>
    public const int Capacity = 100;

    private readonly Lock _gate = new();

    // The one that happened last first, as the console lists them.
    private readonly List<Incident> _incidents = [];
    private readonly Dictionary<RoundId, int> _roundFailures = [];
    private int _lastId;
    private IncidentList _current = new(Version: 0, []);

    /// <summary>Every incident kept, the one that happened last first.</summary>
    public IncidentList Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <summary>
    /// Records an occurrence, and returns the list that includes it.
    /// </summary>
    /// <param name="code">What went wrong.</param>
    /// <param name="round">The round in progress, if any.</param>
    /// <param name="role">The role whose snapshot could not be projected, if that is what went wrong.</param>
    public IncidentList Record(IncidentCode code, RoundInfo? round, Role? role = null)
    {
        var now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        lock (_gate)
        {
            var index = _incidents.FindIndex(i => i.Code == code && i.Round?.RoundId == round?.RoundId && i.Role == role);
            Incident incident;
            if (index >= 0)
            {
                incident = _incidents[index] with { Count = _incidents[index].Count + 1, LastOccurredAt = now };
                _incidents.RemoveAt(index);
            }
            else
            {
                incident = new Incident(++_lastId, code, round, role, Count: 1, now);
            }

            _incidents.Insert(0, incident);
            if (_incidents.Count > Capacity)
            {
                _incidents.RemoveAt(_incidents.Count - 1);
            }

            if (code == IncidentCode.RoundHandlerFailed && round is not null)
            {
                _roundFailures[round.RoundId] = _roundFailures.GetValueOrDefault(round.RoundId) + 1;
            }

            _current = new IncidentList(_current.Version + 1, [.. _incidents]);
            return _current;
        }
    }

    /// <summary>
    /// How many inputs failed in a round, whether or not its incident is still kept: a round that keeps failing may have to
    /// be skipped.
    /// </summary>
    public int FailureCount(RoundId roundId)
    {
        lock (_gate)
        {
            return _roundFailures.GetValueOrDefault(roundId);
        }
    }
}
