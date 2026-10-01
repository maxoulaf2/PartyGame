using PartyGame.Engine;
using PartyGame.Server.Games;

namespace PartyGame.Server.Tests.Games;

internal sealed class RecordingListener(Journal journal, string name = "listener", bool fails = false) : IGameStateListener
{
    public List<GameState> States { get; } = [];

    public ValueTask OnStateChangedAsync(GameState state, CancellationToken cancellationToken)
    {
        if (fails)
        {
            throw new InvalidOperationException("Injected listener failure");
        }

        States.Add(state);
        journal.Add($"notify:{name}");
        return ValueTask.CompletedTask;
    }
}
