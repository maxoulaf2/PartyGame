using System.Text.Json.Nodes;

namespace PartyGame.Tests.Shared.Leaks;

/// <summary>
/// Leak assertions on the snapshots a client received over the wire, for the integration tests of the hub. The tests of
/// the projections of a state use <see cref="LeakSuite{TState, TPhase}"/>.
/// </summary>
internal static class LeakAssert
{
    /// <summary>
    /// Fails if a snapshot received by <paramref name="viewer"/> contains a secret hidden from it, naming the snapshot by
    /// its version and phase.
    /// </summary>
    /// <param name="viewer">The client that received the snapshots.</param>
    /// <param name="json">The snapshots, as received: at least one, or the test would prove nothing.</param>
    /// <param name="secrets">The secrets to look for.</param>
    public static void NoSecretReceived(Viewer viewer, IEnumerable<string> json, params Secret[] secrets)
    {
        var snapshots = json.ToList();
        Assert.True(snapshots.Count > 0, $"{viewer} received no snapshot: there is nothing to look for a leak in.");

        List<string> failures = [];
        foreach (var snapshot in snapshots.Select(s => JsonNode.Parse(s)))
        {
            var context = $"Snapshot version {snapshot?["version"]} (phase {snapshot?["phase"]}), {viewer}";
            failures.AddRange(Leaks.SecretsShown(viewer, snapshot, secrets).Select(finding => $"{context}: {finding}"));
        }

        Fail("Secrets received", failures);
    }

    /// <summary>
    /// Fails with every failure found, one per line, if there is any.
    /// </summary>
    public static void Fail(string title, IReadOnlyCollection<string> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        if (failures.Count > 0)
        {
            Assert.Fail($"{title}:{Environment.NewLine}{string.Join(Environment.NewLine, failures.Select(f => $"- {f}"))}");
        }
    }
}
