using PartyGame.Engine.Inputs;
using PartyGame.Server.Games;

namespace PartyGame.Server.Tests.Games;

/// <summary>
/// Keeps the inputs written to it instead of handing them to a loop.
/// </summary>
internal sealed class RecordingInputWriter(bool fails = false) : IGameInputWriter
{
    private readonly Lock _gate = new();
    private readonly List<GameInput> _inputs = [];

    public IReadOnlyList<GameInput> Inputs
    {
        get
        {
            lock (_gate)
            {
                return [.. _inputs];
            }
        }
    }

    public ValueTask WriteAsync(GameInput input, CancellationToken cancellationToken)
    {
        if (fails)
        {
            throw new InvalidOperationException("Injected writer failure");
        }

        lock (_gate)
        {
            _inputs.Add(input);
        }

        return ValueTask.CompletedTask;
    }

    public Task<InputOutcome> SubmitAsync(GameInput input, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Timers never wait for the outcome of their input.");
}
