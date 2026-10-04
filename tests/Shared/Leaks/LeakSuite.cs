using PartyGame.Contracts;

namespace PartyGame.Tests.Shared.Leaks;

/// <summary>
/// The leak tests of the projections of a state, shared by the lobby and every game mode. A mode describes its
/// scenarios, its secrets and its pairs of states that differ only by a secret; the suite checks that no projection shows
/// a secret to a viewer it is hidden from, and that every phase is covered for every role, so that a phase added later
/// cannot escape the tests.
/// </summary>
/// <remarks>
/// The projection of the game master is only checked against the secrets hidden from <see cref="Audience.Everyone"/>,
/// such as the tokens: it is the only one allowed to hold the answers before their reveal.
/// </remarks>
/// <typeparam name="TState">The state the projections are made from.</typeparam>
/// <typeparam name="TPhase">The phases the mode declares, each of which needs a scenario.</typeparam>
internal sealed class LeakSuite<TState, TPhase> : ILeakSuite<TState>
    where TPhase : struct, Enum
{
    /// <summary>The phase of a state, as the mode sees it.</summary>
    public required Func<TState, TPhase> PhaseOf { get; init; }

    /// <summary>What each viewer receives for a state.</summary>
    public required Func<TState, ProjectionSet> Project { get; init; }

    /// <summary>States that cover every phase, with players, each named for the failure messages.</summary>
    public required IReadOnlyList<(string Name, TState State)> Scenarios { get; init; }

    /// <summary>The secrets of a state: what each projection of it may not show, and to whom.</summary>
    public Func<TState, IEnumerable<Secret>> SecretsOf { get; init; } = _ => [];

    /// <summary>Pairs of states that differ only by a secret. Their states count as scenarios as well.</summary>
    public IReadOnlyList<SecretPair<TState>> Pairs { get; init; } = [];

    /// <summary>
    /// Fails if a phase of <typeparamref name="TPhase"/> is not covered, for one of the roles, by any scenario.
    /// </summary>
    public void AssertEveryPhaseIsCovered()
    {
        var covered = States()
            .SelectMany(s => Project(s.State).All.Select(p => (Phase: PhaseOf(s.State), p.Viewer.Role)))
            .ToHashSet();
        var failures = Enum.GetValues<TPhase>()
            .SelectMany(phase => Enum.GetValues<Role>().Select(role => (Phase: phase, Role: role)))
            .Where(pair => !covered.Contains(pair))
            .Select(pair => $"Phase {pair.Phase}, {pair.Role}: no scenario covers it")
            .ToList();

        LeakAssert.Fail("Phases not covered", failures);
    }

    /// <summary>
    /// Fails if a projection of a scenario contains a secret of its state hidden from its viewer.
    /// </summary>
    public void AssertNoSecretIsShown()
    {
        List<string> failures = [];
        foreach (var (name, state) in States())
        {
            var secrets = SecretsOf(state).ToList();
            foreach (var (viewer, json) in Project(state).All)
            {
                var context = $"Scenario \"{name}\" (phase {PhaseOf(state)}), {viewer}";
                failures.AddRange(Leaks.SecretsShown(viewer, json, secrets).Select(finding => $"{context}: {finding}"));
            }
        }

        LeakAssert.Fail("Secrets shown", failures);
    }

    /// <summary>
    /// Fails if the projection of a viewer a secret is hidden from differs between the two states of its pair.
    /// </summary>
    public void AssertPairsLookTheSame()
    {
        List<string> failures = [];
        foreach (var pair in Pairs)
        {
            var phase = PhaseOf(pair.One);
            if (!EqualityComparer<TPhase>.Default.Equals(phase, PhaseOf(pair.Other)))
            {
                failures.Add($"Pair \"{pair.Secret}\": its states are in phases {phase} and {PhaseOf(pair.Other)}, they must differ only by the secret");
                continue;
            }

            var (one, other) = (Project(pair.One), Project(pair.Other));
            var viewers = one.All.Concat(other.All).Select(p => p.Viewer).Distinct().Where(pair.HiddenFrom.Includes).ToList();
            if (viewers.Count == 0)
            {
                failures.Add($"Pair \"{pair.Secret}\" (phase {phase}): no viewer is in {pair.HiddenFrom}, the pair compares nothing");
            }

            foreach (var viewer in viewers)
            {
                var difference = (one.Has(viewer), other.Has(viewer)) switch
                {
                    (true, true) => Leaks.FirstDifference(one.For(viewer), other.For(viewer)),
                    _ => "the viewer is only in one state",
                };
                if (difference is not null)
                {
                    failures.Add($"Pair \"{pair.Secret}\" (phase {phase}), {viewer}: {difference}");
                }
            }
        }

        LeakAssert.Fail("Secrets told apart", failures);
    }

    /// <inheritdoc />
    public IEnumerable<(string Name, TState State)> States() =>
        Scenarios.Concat(Pairs.SelectMany(p => new[] { ($"{p.Secret}, one", p.One), ($"{p.Secret}, other", p.Other) }));
}
