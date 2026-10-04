using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Bots;

/// <summary>
/// A simulated player that behaves as a phone does: it registers, keeps its token, comes back with it after a lost
/// connection, numbers its intents and sends again the one never acknowledged. It knows only its own projection.
/// </summary>
/// <remarks>
/// Everything happens in <see cref="RunAsync"/>, one wake-up at a time: the handlers of the connection only record what
/// arrives, since a call to the hub awaited inside one would block the reception of its own answer.
/// </remarks>
internal sealed class BotPlayer : IAsyncDisposable
{
    private const int ClockSamples = 5;

    private readonly HubConnection _connection;
    private readonly Uri _serverUrl;
    private readonly BotBehavior _behavior;
    private readonly BotStats _stats;
    private readonly Random _random;
    private readonly TimeSpan _flakyPeriod;

    // A welcome, or null for anything else that may give the bot something to do: a snapshot, an answer due.
    private readonly Channel<Welcome?> _wakeUps = Channel.CreateUnbounded<Welcome?>();
    private readonly Lock _gate = new();
    private PlayerSnapshot? _snapshot;
    private SentIntent? _unacknowledged;

    private string? _token;
    private long _clientSeq;
    private string? _identifiedConnection;
    private long _clockOffsetMs;
    private (RoundId Round, int Question)? _question;
    private long? _answerAt;

    /// <param name="serverUrl">The address of the server.</param>
    /// <param name="nickname">The nickname the bot registers under.</param>
    /// <param name="behavior">How it plays.</param>
    /// <param name="stats">What it does is counted there.</param>
    /// <param name="flakyPeriod">How long a <see cref="BotBehavior.Flaky"/> bot stays connected, on average.</param>
    /// <param name="configure">Changes the transport, for a test server reached in memory.</param>
    public BotPlayer(
        Uri serverUrl,
        string nickname,
        BotBehavior behavior,
        BotStats stats,
        TimeSpan? flakyPeriod = null,
        Action<HttpConnectionOptions>? configure = null)
    {
        _serverUrl = serverUrl;
        Nickname = nickname;
        _behavior = behavior;
        _stats = stats;
        _random = new Random();
        _flakyPeriod = flakyPeriod ?? TimeSpan.FromSeconds(8);
        _connection = BotHub.Create(serverUrl, configure);
        _connection.On<Welcome>(nameof(IGameClient.ReceiveWelcome), welcome => _wakeUps.Writer.TryWrite(welcome));
        _connection.On<PlayerSnapshot>(nameof(IGameClient.ReceivePlayerSnapshot), Receive);
    }

    public string Nickname { get; }

    public BotBehavior Behavior => _behavior;

    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    /// <summary>The newest snapshot received.</summary>
    public PlayerSnapshot? Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    private long ServerNow => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _clockOffsetMs;

    /// <summary>
    /// Connects, then plays until cancelled.
    /// </summary>
    /// <exception cref="BotException">The server cannot be reached, or refuses the registration.</exception>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await BotHub.StartAsync(_connection, _serverUrl, cancellationToken).ConfigureAwait(false);
        if (_behavior == BotBehavior.Flaky)
        {
            // Ends with the cancellation, as the loop below does.
            _ = FlakeAsync(cancellationToken);
        }

        await foreach (var welcome in _wakeUps.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                if (welcome is null)
                {
                    await PlayAsync(cancellationToken).ConfigureAwait(false);
                }
                else if (!welcome.GamePending && _identifiedConnection != _connection.ConnectionId)
                {
                    await IdentifyAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not BotException && !cancellationToken.IsCancellationRequested)
            {
                // The connection was lost meanwhile, maybe already replaced: the welcome of the next one starts over, and
                // the intent never acknowledged is sent again.
            }
        }
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    /// <summary>
    /// Registers, or resumes the session of the token after a lost connection, then sends again the intent never
    /// acknowledged: the server ignores it if it was handled already.
    /// </summary>
    private async Task IdentifyAsync(CancellationToken cancellationToken)
    {
        await SyncClockAsync(cancellationToken).ConfigureAwait(false);

        if (_token is not null)
        {
            var resumed = await _connection
                .InvokeAsync<ResumeSessionResult>("ResumeSession", new ResumeSessionRequest(_token), cancellationToken)
                .ConfigureAwait(false);
            switch (resumed.Refusal)
            {
                case null:
                    _stats.Reconnected();
                    break;
                case ResumeSessionRefusal.SessionUnknown:
                    // The game master started a new game: the bot registers in it.
                    _token = null;
                    break;
                default:
                    // Waiting for the game master to resolve the saved game, or lost meanwhile: welcomed again later.
                    return;
            }
        }

        if (_token is null)
        {
            var joined = await _connection
                .InvokeAsync<JoinResult>("JoinGame", new JoinRequest(Nickname), cancellationToken)
                .ConfigureAwait(false);
            if (joined.Refusal == JoinRefusal.GamePending)
            {
                return;
            }

            if (joined.Refusal is { } refusal)
            {
                throw new BotException($"{Nickname} : inscription refusée ({refusal}).");
            }

            _token = joined.Token;
            _clientSeq = 0;
            lock (_gate)
            {
                _unacknowledged = null;
            }
        }

        _identifiedConnection = _connection.ConnectionId;
        if (Unacknowledged() is { } intent)
        {
            await SendAsync(intent, cancellationToken).ConfigureAwait(false);
        }

        await PlayAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the intent the bot's behavior calls for now, if any, and schedules a wake-up when it calls for one later.
    /// </summary>
    private async Task PlayAsync(CancellationToken cancellationToken)
    {
        if (_identifiedConnection != _connection.ConnectionId || Unacknowledged() is not null)
        {
            return;
        }

        if (Snapshot is not { Round: { } round } snapshot)
        {
            return;
        }

        // One strategy per game mode; a mode the bots do not know leaves them waiting.
        PlayerRoundIntent intent;
        Func<PlayerSnapshot, bool> reflects;
        switch (snapshot.RoundView)
        {
            case QuizPlayerView view when QuizBot.CanAnswer(view):
                if (!IsDue((round.RoundId, view.QuestionNumber), now => QuizBot.AnswerAt(view, _behavior, now, _random)))
                {
                    return;
                }

                var answer = QuizBot.Answer(round.RoundId, view, _behavior, _random);
                (intent, reflects) = (answer, s => QuizBot.Reflects(s, answer));
                break;
            default:
                return;
        }

        var sent = new SentIntent(++_clientSeq, intent, reflects);
        lock (_gate)
        {
            _unacknowledged = sent;
        }

        await SendAsync(sent, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Whether the bot acts on the question now: the time is drawn once per question, as soon as the behavior can tell it,
    /// and a wake-up is scheduled for it.
    /// </summary>
    private bool IsDue((RoundId, int) question, Func<long, long?> actAt)
    {
        if (_question != question)
        {
            (_question, _answerAt) = (question, null);
        }

        var now = ServerNow;
        if (_answerAt is null && actAt(now) is { } at)
        {
            _answerAt = at;
            _ = Task.Delay(TimeSpan.FromMilliseconds(Math.Max(0, at - now)))
                .ContinueWith(_ => _wakeUps.Writer.TryWrite(null), TaskScheduler.Default);
        }

        return now >= _answerAt;
    }

    /// <summary>
    /// Sends the intent, and returns once the hub acknowledged it. The snapshots it caused arrive before the
    /// acknowledgment: if none reflects it by then, the server rejected it.
    /// </summary>
    private async Task SendAsync(SentIntent sent, CancellationToken cancellationToken)
    {
        sent.Clock.Restart();
        _stats.IntentSent();
        await _connection
            .InvokeAsync("SendRoundIntent", JsonSerializer.SerializeToElement(new PlayerIntentEnvelope(sent.ClientSeq, sent.Intent), ContractJsonOptions.Default), cancellationToken)
            .ConfigureAwait(false);

        lock (_gate)
        {
            if (_unacknowledged == sent)
            {
                _unacknowledged = null;
                if (_snapshot is null || !sent.Reflects(_snapshot))
                {
                    _stats.IntentRejected();
                }
            }
        }
    }

    /// <summary>
    /// Keeps the newest snapshot, as a phone does, and measures the delay of the intent it reflects.
    /// </summary>
    private void Receive(PlayerSnapshot snapshot)
    {
        lock (_gate)
        {
            // One of another game is newer whatever its version: the game master started a new one.
            if (_snapshot is not null && snapshot.GameId == _snapshot.GameId && snapshot.Version <= _snapshot.Version)
            {
                return;
            }

            _snapshot = snapshot;
            if (_unacknowledged is { Measured: false } sent && sent.Reflects(snapshot))
            {
                sent.Measured = true;
                _stats.BroadcastDelay(sent.Clock.Elapsed);
            }
        }

        _wakeUps.Writer.TryWrite(null);
    }

    /// <summary>
    /// Estimates the offset of the server clock as the phones do: the sample of the shortest round trip is the most
    /// accurate.
    /// </summary>
    private async Task SyncClockAsync(CancellationToken cancellationToken)
    {
        var shortest = long.MaxValue;
        for (var sample = 0; sample < ClockSamples; sample++)
        {
            var sentAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var result = await _connection.InvokeAsync<ClockSyncResult>("SyncClock", cancellationToken).ConfigureAwait(false);
            var receivedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (receivedAt - sentAt < shortest)
            {
                shortest = receivedAt - sentAt;
                _clockOffsetMs = result.ServerTime - ((sentAt + receivedAt) / 2);
            }
        }
    }

    /// <summary>
    /// Drops the connection and comes back, at random times, as a phone that goes to sleep or loses the Wi-Fi.
    /// </summary>
    private async Task FlakeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await Task.Delay(_flakyPeriod * (0.5 + _random.NextDouble()), cancellationToken).ConfigureAwait(false);
                await _connection.StopAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(_flakyPeriod * _random.NextDouble() / 4, cancellationToken).ConfigureAwait(false);
                await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // The server is away, or the connection is reconnecting by itself: tried again at the next turn.
            }
        }
    }

    private SentIntent? Unacknowledged()
    {
        lock (_gate)
        {
            return _unacknowledged;
        }
    }

    private sealed class SentIntent(long clientSeq, PlayerRoundIntent intent, Func<PlayerSnapshot, bool> reflects)
    {
        public long ClientSeq => clientSeq;

        public PlayerRoundIntent Intent => intent;

        public Stopwatch Clock { get; } = new();

        public bool Measured { get; set; }

        public bool Reflects(PlayerSnapshot snapshot) => reflects(snapshot);
    }
}
