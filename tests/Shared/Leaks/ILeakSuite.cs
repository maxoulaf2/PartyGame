namespace PartyGame.Tests.Shared.Leaks;

/// <summary>
/// A <see cref="LeakSuite{TState, TPhase}"/> whatever the phases of its mode, so that a test can go through the
/// suites of every mode, such as to check that each of their states survives its persistence.
/// </summary>
/// <typeparam name="TState">The state the projections are made from.</typeparam>
internal interface ILeakSuite<TState>
{
    /// <summary>What each viewer receives for a state.</summary>
    Func<TState, ProjectionSet> Project { get; }

    /// <summary>The scenarios, then both states of each pair, each named for the failure messages.</summary>
    IEnumerable<(string Name, TState State)> States();
}
