using System.Threading.Channels;
using PartyGame.Engine.Inputs;

namespace PartyGame.Server.Games;

/// <summary>
/// The queue of the game: producers (hub, timers) write to it, and the <see cref="GameLoop"/> is its only reader.
/// Kept apart from the loop so that the producers the loop depends on, such as the timers, can write to it too.
/// </summary>
internal sealed class GameInputQueue : IGameInputWriter
{
    // Unbounded: an intent must never be dropped, and the volume of a party game cannot exhaust memory. A write then
    // never waits for room, so it is queued before WriteAsync returns, as IGameInputWriter promises.
    private readonly Channel<QueuedInput> _channel =
        Channel.CreateUnbounded<QueuedInput>(new UnboundedChannelOptions { SingleReader = true });

    /// <summary>
    /// Read side of the queue, for the loop only.
    /// </summary>
    public ChannelReader<QueuedInput> Reader => _channel.Reader;

    public async ValueTask WriteAsync(GameInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        await EnqueueAsync(new QueuedInput(input, Completion: null), cancellationToken).ConfigureAwait(false);
    }

    public async Task<InputOutcome> SubmitAsync(GameInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        // The producer's continuation must not run on the loop, which would then wait for it.
        var completion = new TaskCompletionSource<InputOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        await EnqueueAsync(new QueuedInput(input, completion), cancellationToken).ConfigureAwait(false);
        return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Refuses any new input and cancels the producers still waiting for an answer, once nobody reads the queue anymore.
    /// </summary>
    public void Close(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        while (_channel.Reader.TryRead(out var pending))
        {
            pending.Completion?.TrySetCanceled(cancellationToken);
        }
    }

    private async ValueTask EnqueueAsync(QueuedInput queued, CancellationToken cancellationToken)
    {
        try
        {
            await _channel.Writer.WriteAsync(queued, cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException ex)
        {
            throw new OperationCanceledException("The game loop is stopped.", ex);
        }
    }
}
