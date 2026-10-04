using System.Collections.Immutable;
using PartyGame.Contracts;

namespace PartyGame.Server.Incidents;

/// <summary>
/// The incidents of the server since it started, for the game master. Kept apart from the game state: an incident changes
/// nothing of the game, and is forgotten on restart, the logs keeping their trace. The occurrences of a same code in a same
/// round, for a same role and a same step, make one incident; past <see cref="Capacity"/> incidents, the one that happened
/// longest ago is dropped. An incident whose cause is over, such as a persistence that works again, is forgotten. A round whose inputs failed <see cref="SkipThreshold"/> times is listed as failing, so that the
/// game master is offered to skip it, and so is at once a round the TV screen could not show.
/// </summary>
/// <remarks>
/// Fed by the loop only, but read by the hub when a console is accepted: the lock guards the list against that read.
/// </remarks>
internal sealed class IncidentJournal(TimeProvider timeProvider)
{
    /// <summary>The most incidents kept: enough for a whole evening, small enough to send whole each time.</summary>
    public const int Capacity = 100;

    /// <summary>
    /// How many inputs of a round must fail for the game master to be offered to skip it: a single failure the server
    /// recovered from is no reason to disturb them.
    /// </summary>
    public const int SkipThreshold = 3;

    private readonly Lock _gate = new();

    // The one that happened last first, as the console lists them.
    private readonly List<Incident> _incidents = [];
    private readonly Dictionary<RoundId, int> _roundFailures = [];
    private readonly List<RoundId> _failingRounds = [];
    private int _lastId;
    private IncidentList _current = new(Version: 0, [], []);

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
    /// <param name="step">The step of the round concerned, if that is what the incident names.</param>
    public IncidentList Record(IncidentCode code, RoundInfo? round, Role? role = null, int? step = null)
    {
        var now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        lock (_gate)
        {
            var index = _incidents.FindIndex(i => i.Code == code && i.Round?.RoundId == round?.RoundId && i.Role == role && i.Step == step);
            Incident incident;
            if (index >= 0)
            {
                incident = _incidents[index] with { Count = _incidents[index].Count + 1, LastOccurredAt = now };
                _incidents.RemoveAt(index);
            }
            else
            {
                incident = new Incident(++_lastId, code, round, role, step, Count: 1, now);
            }

            _incidents.Insert(0, incident);
            if (_incidents.Count > Capacity)
            {
                _incidents.RemoveAt(_incidents.Count - 1);
            }

            if (round is not null && IsFailing(code, round.RoundId) && !_failingRounds.Contains(round.RoundId))
            {
                _failingRounds.Add(round.RoundId);
            }

            _current = new IncidentList(_current.Version + 1, [.. _incidents], [.. _failingRounds]);
            return _current;
        }
    }

    /// <summary>
    /// Forgets every incident of a code, once what went wrong is over, and returns the list without them.
    /// </summary>
    /// <param name="code">What no longer goes wrong.</param>
    /// <returns>The list without them, or <see langword="null"/> when no incident had this code: the list is unchanged.</returns>
    public IncidentList? Resolve(IncidentCode code)
    {
        lock (_gate)
        {
            if (_incidents.RemoveAll(i => i.Code == code) == 0)
            {
                return null;
            }

            _current = new IncidentList(_current.Version + 1, [.. _incidents], [.. _failingRounds]);
            return _current;
        }
    }

    /// <summary>
    /// How many inputs failed in a round, whether or not its incident is still kept: a round that keeps failing may have to
    /// be skipped. Each round counts from zero, since each one has an identifier of its own.
    /// </summary>
    public int FailureCount(RoundId roundId)
    {
        lock (_gate)
        {
            return _roundFailures.GetValueOrDefault(roundId);
        }
    }

    /// <summary>
    /// Counts an occurrence against its round, and tells whether the round must be offered to be skipped: a TV screen that
    /// cannot show the round leaves the public without it, so the game master may skip it at once. Called under the lock.
    /// </summary>
    private bool IsFailing(IncidentCode code, RoundId roundId)
    {
        switch (code)
        {
            case IncidentCode.RoundHandlerFailed:
                return (_roundFailures[roundId] = _roundFailures.GetValueOrDefault(roundId) + 1) >= SkipThreshold;
            case IncidentCode.DisplayViewFailed:
                return true;
            default:
                return false;
        }
    }
}
