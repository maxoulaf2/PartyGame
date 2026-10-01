using System.Collections.Immutable;
using System.Threading.Channels;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;

namespace PartyGame.Server.Games;

/// <summary>
/// The only writer of the game state. Inputs from every connection and timer go through one queue and are handled one at
/// a time, so neither the engine nor the state ever needs a lock. No exception ever leaves the loop: a background service
/// that throws stops the whole application.
/// </summary>
internal sealed class GameLoop : BackgroundService, IGameInputWriter
{
    // Unbounded: an intent must never be dropped, and the volume of a party game cannot exhaust memory.
    private readonly Channel<QueuedInput> _queue =
        Channel.CreateUnbounded<QueuedInput>(new UnboundedChannelOptions { SingleReader = true });

    private readonly IGameEngine _engine;
    private readonly TimeProvider _timeProvider;
    private readonly IEffectExecutor _effects;
    private readonly ImmutableArray<IGameStateListener> _listeners;
    private readonly ILogger<GameLoop> _logger;
    private readonly Random _random;
    private GameState _state;

    /// <param name="initialState">State of the game before any input.</param>
    /// <param name="seed">Seed of the random generator handed to the engine, so that a game can be replayed.</param>
    /// <param name="engine">The rules of the game.</param>
    /// <param name="timeProvider">Source of <see cref="GameContext.Now"/>.</param>
    /// <param name="effects">Executes the effects of each transition.</param>
    /// <param name="listeners">Notified after each transition that changed the state.</param>
    /// <param name="logger">Logs rejections and failures.</param>
    public GameLoop(
        GameState initialState,
        int seed,
        IGameEngine engine,
        TimeProvider timeProvider,
        IEffectExecutor effects,
        IEnumerable<IGameStateListener> listeners,
        ILogger<GameLoop> logger)
    {
        _state = initialState;
        _random = new Random(seed);
        _engine = engine;
        _timeProvider = timeProvider;
        _effects = effects;
        _listeners = [.. listeners];
        _logger = logger;
    }

    /// <summary>
    /// The current state. Readable from any thread: it is immutable, and only its reference is replaced by the loop.
    /// </summary>
    public GameState State => Volatile.Read(ref _state);

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.GameCreated(_state.GameId.Value);
        try
        {
            while (await _queue.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (_queue.Reader.TryRead(out var queued))
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        queued.Completion?.TrySetCanceled(stoppingToken);
                        break;
                    }

                    await HandleAsync(queued, stoppingToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        finally
        {
            // Producers waiting for an answer must not hang once nobody reads the queue anymore.
            _queue.Writer.TryComplete();
            while (_queue.Reader.TryRead(out var pending))
            {
                pending.Completion?.TrySetCanceled(stoppingToken);
            }

            _logger.GameLoopStopped();
        }
    }

    private async ValueTask EnqueueAsync(QueuedInput queued, CancellationToken cancellationToken)
    {
        try
        {
            await _queue.Writer.WriteAsync(queued, cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException ex)
        {
            throw new OperationCanceledException("The game loop is stopped.", ex);
        }
    }

    private async ValueTask HandleAsync(QueuedInput queued, CancellationToken stoppingToken)
    {
        try
        {
            var outcome = await ApplyAsync(queued.Input, stoppingToken).ConfigureAwait(false);
            queued.Completion?.TrySetResult(outcome);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            queued.Completion?.TrySetCanceled(stoppingToken);
            throw;
        }
    }

    private async ValueTask<InputOutcome> ApplyAsync(GameInput input, CancellationToken stoppingToken)
    {
        var state = _state;
        Transition transition;
        try
        {
            transition = _engine.Handle(state, input, new GameContext(_timeProvider.GetUtcNow(), _random));
        }
        catch (Exception ex)
        {
            // A bug in the engine must not stop the game. The state is immutable: keeping the previous reference is the rollback.
            _logger.InputFailed(ex, input.GetType().Name, state.Phase);
            return InputOutcome.Failed;
        }

        if (transition.Rejection is { } rejection)
        {
            _logger.InputRejected(input.GetType().Name, rejection);
            return InputOutcome.Rejected(rejection);
        }

        var changed = !ReferenceEquals(transition.State, state);
        if (changed)
        {
            Volatile.Write(ref _state, transition.State);
        }

        foreach (var effect in transition.Effects)
        {
            try
            {
                await _effects.ExecuteAsync(effect, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // A failing effect must prevent neither the other effects nor the notification.
                _logger.EffectFailed(ex, effect.GetType().Name);
            }
        }

        if (changed)
        {
            await NotifyAsync(transition.State, stoppingToken).ConfigureAwait(false);
        }

        return InputOutcome.Accepted;
    }

    private async ValueTask NotifyAsync(GameState state, CancellationToken stoppingToken)
    {
        foreach (var listener in _listeners)
        {
            try
            {
                await listener.OnStateChangedAsync(state, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // A failing listener must not prevent the others from being notified.
                _logger.ListenerFailed(ex, listener.GetType().Name);
            }
        }
    }
}
