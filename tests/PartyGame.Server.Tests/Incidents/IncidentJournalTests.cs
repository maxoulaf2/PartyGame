using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Server.Incidents;

namespace PartyGame.Server.Tests.Incidents;

public sealed class IncidentJournalTests
{
    private static readonly RoundInfo _warmUp = new(new RoundId(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e")), Number: 1, Count: 2, "Échauffement");
    private static readonly RoundInfo _final = new(new RoundId(Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7")), Number: 2, Count: 2, "Finale");

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 3, 21, 0, 0, TimeSpan.Zero));
    private readonly IncidentJournal _journal;

    public IncidentJournalTests() => _journal = new IncidentJournal(_time);

    [Fact]
    public void Current_NothingRecorded_IsEmpty()
    {
        // When
        var current = _journal.Current;

        // Then
        Assert.Equal(0, current.Version);
        Assert.Empty(current.Incidents);
    }

    [Fact]
    public void Record_FirstOccurrence_ListsItOnce()
    {
        // When
        var list = _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp);

        // Then
        Assert.Equal(1, list.Version);
        Assert.Equal(new Incident(1, IncidentCode.RoundHandlerFailed, _warmUp, Role: null, Step: null, Count: 1, _time.GetUtcNow().ToUnixTimeMilliseconds()), Assert.Single(list.Incidents));
        Assert.Same(list, _journal.Current);
    }

    [Fact]
    public void Record_SameCodeInSameRound_CountsOccurrencesOnOneLineAtTheTimeOfTheLast()
    {
        // Given
        _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp);
        _journal.Record(IncidentCode.EffectFailed, _warmUp);
        _time.Advance(TimeSpan.FromMinutes(2));

        // When
        var list = _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp);

        // Then: the incident repeated comes first again
        Assert.Equal(3, list.Version);
        Assert.Equal(
            [(1, IncidentCode.RoundHandlerFailed, 2, _time.GetUtcNow().ToUnixTimeMilliseconds()), (2, IncidentCode.EffectFailed, 1, _time.GetUtcNow().AddMinutes(-2).ToUnixTimeMilliseconds())],
            list.Incidents.Select(i => (i.Id, i.Code, i.Count, i.LastOccurredAt)));
    }

    [Fact]
    public void Record_SameCodeInAnotherRoundOrForAnotherRole_ListsItApart()
    {
        // When
        _journal.Record(IncidentCode.ProjectionFailed, _warmUp, Role.Display);
        _journal.Record(IncidentCode.ProjectionFailed, _warmUp, Role.Player);
        _journal.Record(IncidentCode.ProjectionFailed, _final, Role.Display);
        var list = _journal.Record(IncidentCode.ProjectionFailed, round: null, Role.Display);

        // Then
        Assert.Equal(
            [(4, (RoundInfo?)null, Role.Display), (3, _final, Role.Display), (2, _warmUp, Role.Player), (1, _warmUp, Role.Display)],
            list.Incidents.Select(i => (i.Id, i.Round, i.Role)));
        Assert.All(list.Incidents, i => Assert.Equal(1, i.Count));
    }

    [Fact]
    public void Record_BeyondCapacity_DropsTheIncidentThatHappenedLongestAgo()
    {
        // Given: the first incident happened last
        _journal.Record(IncidentCode.EffectFailed, round: null);
        for (var number = 1; number < IncidentJournal.Capacity; number++)
        {
            _journal.Record(IncidentCode.RoundHandlerFailed, new RoundInfo(new RoundId(Guid.NewGuid()), number, IncidentJournal.Capacity, "Manche"));
        }

        _journal.Record(IncidentCode.EffectFailed, round: null);
        var oldest = _journal.Current.Incidents[^1];

        // When
        var list = _journal.Record(IncidentCode.RoundHandlerFailed, _final);

        // Then
        Assert.Equal(IncidentJournal.Capacity, list.Incidents.Length);
        Assert.DoesNotContain(oldest, list.Incidents);
        Assert.Equal((IncidentCode.RoundHandlerFailed, _final), (list.Incidents[0].Code, list.Incidents[0].Round));
        Assert.Contains(list.Incidents, i => i is { Code: IncidentCode.EffectFailed, Count: 2 });
    }

    [Fact]
    public void FailureCount_InputsFailedInRounds_CountsThoseOfEachRound()
    {
        // When
        _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp);
        _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp);
        _journal.Record(IncidentCode.EffectFailed, _warmUp);
        _journal.Record(IncidentCode.ProjectionFailed, _warmUp, Role.Player);
        _journal.Record(IncidentCode.RoundHandlerFailed, _final);

        // Then
        Assert.Equal((2, 1), (_journal.FailureCount(_warmUp.RoundId), _journal.FailureCount(_final.RoundId)));
        Assert.Equal(0, _journal.FailureCount(new RoundId(Guid.NewGuid())));
    }

    [Fact]
    public void Record_RoundFailingUpToTheThreshold_ListsItAsFailingOnce()
    {
        // When
        var lists = Enumerable.Range(0, IncidentJournal.SkipThreshold + 1)
            .Select(_ => _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp))
            .ToList();

        // Then: not before the threshold, and once past it
        Assert.All(lists.Take(IncidentJournal.SkipThreshold - 1), list => Assert.Empty(list.FailingRounds));
        Assert.Equal([_warmUp.RoundId], lists[IncidentJournal.SkipThreshold - 1].FailingRounds);
        Assert.Equal([_warmUp.RoundId], lists[^1].FailingRounds);
    }

    [Fact]
    public void Record_OtherIncidentsOfARound_DoNotMakeItFailing()
    {
        // When: only the inputs that fail count, not what follows them
        for (var i = 0; i < IncidentJournal.SkipThreshold; i++)
        {
            _journal.Record(IncidentCode.EffectFailed, _warmUp);
            _journal.Record(IncidentCode.ProjectionFailed, _warmUp, Role.Display);
            _journal.Record(IncidentCode.RoundHandlerFailed, round: null);
        }

        // Then
        Assert.Empty(_journal.Current.FailingRounds);
    }

    [Fact]
    public void Record_FailuresOfSeveralRounds_CountsEachRoundFromZero()
    {
        // Given: the warm-up failed until the game master skipped it
        for (var i = 0; i < IncidentJournal.SkipThreshold; i++)
        {
            _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp);
        }

        // When: the final fails once less
        for (var i = 0; i < IncidentJournal.SkipThreshold - 1; i++)
        {
            _journal.Record(IncidentCode.RoundHandlerFailed, _final);
        }

        // Then
        Assert.Equal([_warmUp.RoundId], _journal.Current.FailingRounds);
        _journal.Record(IncidentCode.RoundHandlerFailed, _final);
        Assert.Equal([_warmUp.RoundId, _final.RoundId], _journal.Current.FailingRounds);
    }

    [Fact]
    public void Record_SameCodeForAnotherStep_ListsItApart()
    {
        // When
        _journal.Record(IncidentCode.DisplayMediaFailed, _warmUp, step: 2);
        _journal.Record(IncidentCode.DisplayMediaFailed, _warmUp, step: 3);
        var list = _journal.Record(IncidentCode.DisplayMediaFailed, _warmUp, step: 2);

        // Then
        Assert.Equal([(2, 2), (3, 1)], list.Incidents.Select(i => (i.Step!.Value, i.Count)));
    }

    [Fact]
    public void Record_DisplayViewFailedInARound_ListsItAsFailingAtOnce()
    {
        // When
        var list = _journal.Record(IncidentCode.DisplayViewFailed, _warmUp);

        // Then: the public no longer sees the round, so the game master may skip it without waiting
        Assert.Equal([_warmUp.RoundId], list.FailingRounds);
        Assert.Equal(0, _journal.FailureCount(_warmUp.RoundId));
    }

    [Fact]
    public void Record_RoundFailingOnBothSides_ListsItOnce()
    {
        // Given
        _journal.Record(IncidentCode.DisplayViewFailed, _warmUp);

        // When
        for (var i = 0; i < IncidentJournal.SkipThreshold; i++)
        {
            _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp);
        }

        _journal.Record(IncidentCode.DisplayViewFailed, _warmUp);

        // Then
        Assert.Equal([_warmUp.RoundId], _journal.Current.FailingRounds);
    }

    [Fact]
    public void Record_DisplayFailuresOutsideARoundOrOfAMedia_DoNotMakeAnyRoundFailing()
    {
        // When
        _journal.Record(IncidentCode.DisplayViewFailed, round: null);
        _journal.Record(IncidentCode.DisplayMediaFailed, _warmUp, step: 1);

        // Then
        Assert.Empty(_journal.Current.FailingRounds);
    }

    [Fact]
    public void Resolve_IncidentOfTheCode_ForgetsItAndTellsTheChange()
    {
        // Given
        _journal.Record(IncidentCode.PersistenceFailed, round: null);
        _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp);

        // When
        var list = _journal.Resolve(IncidentCode.PersistenceFailed);

        // Then: the console takes the list, newer than the last
        Assert.NotNull(list);
        Assert.Equal(3, list.Version);
        Assert.Equal(IncidentCode.RoundHandlerFailed, Assert.Single(list.Incidents).Code);
        Assert.Same(list, _journal.Current);
    }

    [Fact]
    public void Resolve_NoIncidentOfTheCode_LeavesTheListUnchanged()
    {
        // Given
        var before = _journal.Record(IncidentCode.RoundHandlerFailed, _warmUp);

        // When
        var list = _journal.Resolve(IncidentCode.PersistenceFailed);

        // Then
        Assert.Null(list);
        Assert.Same(before, _journal.Current);
    }
}
