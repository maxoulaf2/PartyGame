using PartyGame.Engine.Effects;
using PartyGame.Server.Games;

namespace PartyGame.Server.Tests.Games;

internal sealed class RecordingEffectExecutor(Journal journal, Func<Effect, bool>? fails = null) : IEffectExecutor
{
    public ValueTask ExecuteAsync(Effect effect, CancellationToken cancellationToken)
    {
        if (fails?.Invoke(effect) == true)
        {
            throw new InvalidOperationException("Injected effect failure");
        }

        journal.Add($"effect:{effect}");
        return ValueTask.CompletedTask;
    }
}
