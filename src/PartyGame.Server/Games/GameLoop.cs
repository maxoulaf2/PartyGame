using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Server.Incidents;

namespace PartyGame.Server.Games;

/// <summary>
/// The only writer of the game state. Inputs from every connection and timer go through one queue and are handled one at
/// a time, so neither the engine nor the state ever needs a lock. No exception ever leaves the loop: a background service
/// that throws stops the whole application. A failure the loop recovers from is logged, and reported to the game master as
/// an incident.
/// </summary>
internal sealed class GameLoop : BackgroundService
{
    private readonly GameInputQueue _queue;
    private readonly IGameEngine _engine;
    private readonly TimeProvider _timeProvider;
    private readonly IEffectExecutor _effects;
    private readonly ImmutableArray<IGameStateListener> _listeners;
    private readonly IIncidentReporter _incidents;
    private readonly ILogger<GameLoop> _logger;
    private readonly Random _random;
    private GameState _state;

    /// <param name="initialState">State of the game before any input.</param>
    /// <param name="seed">Seed of the random generator handed to the engine, so that a game can be replayed.</param>
    /// <param name="queue">The queue the loop reads its inputs from.</param>
    /// <param name="engine">The rules of the game.</param>
    /// <param name="timeProvider">Source of <see cref="GameContext.Now"/>.</param>
    /// <param name="effects">Executes the effects of each transition.</param>
    /// <param name="listeners">Notified after each transition that changed the state.</param>
    /// <param name="incidents">Tells the game master about the failures the loop recovered from.</param>
    /// <param name="logger">Logs rejections and failures.</param>
    public GameLoop(
        GameState initialState,
        int seed,
        GameInputQueue queue,
        IGameEngine engine,
        TimeProvider timeProvider,
        IEffectExecutor effects,
        IEnumerable<IGameStateListener> listeners,
        IIncidentReporter incidents,
        ILogger<GameLoop> logger)
    {
        _state = initialState;
        _random = new Random(seed);
        _queue = queue;
        _engine = engine;
        _timeProvider = timeProvider;
        _effects = effects;
        _listeners = [.. listeners];
        _incidents = incidents;
        _logger = logger;
    }

    /// <summary>
    /// The current state. Readable from any thread: it is immutable, and only its reference is replaced by the loop.
    /// </summary>
    public GameState State => Volatile.Read(ref _state);

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
            _queue.Close(stoppingToken);

            _logger.GameLoopStopped();
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
            await ReportAsync(IncidentCode.RoundHandlerFailed, state, stoppingToken).ConfigureAwait(false);
            return InputOutcome.Failed;
        }

        if (transition.Rejection is { } rejection)
        {
            _logger.InputRejected(input.GetType().Name, rejection);
            return InputOutcome.Rejected(rejection);
        }

        var changed = !ReferenceEquals(transition.State, state);
        var newState = state;
        if (changed)
        {
            // Numbered here rather than by the engine, so that no transition can forget it: clients rely on it to
            // ignore older snapshots.
            newState = transition.State with { Version = state.Version + 1 };
            Volatile.Write(ref _state, newState);
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
                await ReportAsync(IncidentCode.EffectFailed, newState, stoppingToken).ConfigureAwait(false);
            }
        }

        if (changed)
        {
            await NotifyAsync(newState, stoppingToken).ConfigureAwait(false);
        }

        if (input is ResumeSavedGame && state.PendingGame is { } pending)
        {
            // Before any other input, so that none is judged against the deadlines of before the stop. Handled on its own,
            // so that a round failing to resume leaves the game resumed, with an incident naming the round.
            await ApplyAsync(new GameResumed(pending.SavedAt), stoppingToken).ConfigureAwait(false);
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
                // A failing listener must not prevent the others from being notified. For the game master, it is one
                // more thing that follows the change and failed, like an effect.
                _logger.ListenerFailed(ex, listener.GetType().Name);
                await ReportAsync(IncidentCode.EffectFailed, state, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask ReportAsync(IncidentCode code, GameState state, CancellationToken stoppingToken)
    {
        try
        {
            await _incidents.ReportAsync(code, state, role: null, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // The failure itself is logged already: the game goes on without its incident.
            _logger.IncidentNotReported(ex, code);
        }
    }
}
