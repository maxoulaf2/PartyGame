namespace PartyGame.Server.Tests.Games;

/// <summary>
/// What the effect executors and listeners saw, in order.
/// </summary>
internal sealed class Journal
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void Add(string entry) => _entries.Add(entry);
}
