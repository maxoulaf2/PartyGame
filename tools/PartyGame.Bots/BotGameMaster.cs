using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Bots;

/// <summary>
/// A simulated game master that runs the game alone, from its snapshots as the console shows them: it chooses the pack,
/// starts once the players are there, then takes each step of each round, up to the final ranking.
/// </summary>
/// <remarks>
/// Every step names the one it moves on from: a step sent from an outdated snapshot is rejected by the server, and the
/// newer snapshot tells the next one.
/// </remarks>
internal sealed class BotGameMaster : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly Uri _serverUrl;
    private readonly string _code;
    private readonly string? _packId;
    private readonly int _playerCount;
    private readonly TimeSpan _stepDelay;

    // True for a welcome, false for a snapshot.
    private readonly Channel<bool> _wakeUps = Channel.CreateUnbounded<bool>();
    private readonly Lock _gate = new();
    private GameMasterSnapshot? _snapshot;

    /// <param name="serverUrl">The address of the server.</param>
    /// <param name="code">The game master code the server console shows.</param>
    /// <param name="packId">The pack to play, or null for the first valid one.</param>
    /// <param name="playerCount">How many connected players to wait for before starting.</param>
    /// <param name="stepDelay">The pause before each step, for the screens to be read.</param>
    /// <param name="configure">Changes the transport, for a test server reached in memory.</param>
    public BotGameMaster(
        Uri serverUrl,
        string code,
        string? packId,
        int playerCount,
        TimeSpan stepDelay,
        Action<HttpConnectionOptions>? configure = null)
    {
        _serverUrl = serverUrl;
        _code = code;
        _packId = packId;
        _playerCount = playerCount;
        _stepDelay = stepDelay;
        _connection = BotHub.Create(serverUrl, configure);
        _connection.On<Welcome>(nameof(IGameClient.ReceiveWelcome), _ => _wakeUps.Writer.TryWrite(true));
        _connection.On<GameMasterSnapshot>(nameof(IGameClient.ReceiveGameMasterSnapshot), Receive);
    }

    /// <summary>The newest snapshot received.</summary>
    public GameMasterSnapshot? Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    /// <summary>
    /// Connects, then runs the game until its final ranking.
    /// </summary>
    /// <exception cref="BotException">The server cannot be reached, refuses the code or the pack.</exception>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await BotHub.StartAsync(_connection, _serverUrl, cancellationToken).ConfigureAwait(false);

        long? actedOn = null;
        await foreach (var welcome in _wakeUps.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                if (welcome)
                {
                    // Every new connection, and the end of a saved game's resolution: the snapshot that follows tells the step.
                    await AnnounceAsync(cancellationToken).ConfigureAwait(false);
                    actedOn = null;
                    continue;
                }

                // The newest snapshot is read after the pause: those queued meanwhile need no pause of their own.
                await Task.Delay(_stepDelay, cancellationToken).ConfigureAwait(false);
                while (_wakeUps.Reader.TryPeek(out var next) && !next)
                {
                    _wakeUps.Reader.TryRead(out _);
                }

                if (Snapshot is not { } snapshot || snapshot.Version == actedOn)
                {
                    continue;
                }

                if (snapshot.Phase == Phase.Finished)
                {
                    return;
                }

                actedOn = snapshot.Version;
                await StepAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not BotException && !cancellationToken.IsCancellationRequested)
            {
                // The connection was lost meanwhile: the welcome of the next one starts over.
            }
        }
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    private async Task AnnounceAsync(CancellationToken cancellationToken)
    {
        var result = await _connection
            .InvokeAsync<AnnouncementResult>("Announce", new Announcement(Role.GameMaster, _code), cancellationToken)
            .ConfigureAwait(false);
        if (result.Refusal == AnnouncementRefusal.GameMasterCodeInvalid)
        {
            throw new BotException("Code GM refusé : utilisez celui qu'affiche la console du serveur.");
        }
    }

    private async Task StepAsync(GameMasterSnapshot snapshot, CancellationToken cancellationToken)
    {
        switch (snapshot)
        {
            case { Phase: Phase.Lobby }:
                await PrepareAsync(snapshot, cancellationToken).ConfigureAwait(false);
                break;

            // One strategy per game mode; a mode the bot does not know leaves the game to a human console.
            case { Phase: Phase.Round, Round: { } round, RoundView: QuizGameMasterView view }
                when QuizBot.NextStep(round.RoundId, view) is { } step:
                await _connection
                    .InvokeAsync("SendGameMasterRoundIntent", JsonSerializer.SerializeToElement(step, ContractJsonOptions.Default), cancellationToken)
                    .ConfigureAwait(false);
                break;
            case { Phase: Phase.BetweenRounds, Round: { } finished }:
                await _connection.InvokeAsync("NextRound", new NextRoundRequest(finished.RoundId), cancellationToken).ConfigureAwait(false);
                break;
            default:
                // Answers open, or a saved game for a human to resolve.
                break;
        }
    }

    /// <summary>
    /// Chooses the pack, then starts once every expected player is connected. A start refused shows in the next snapshot.
    /// </summary>
    private async Task PrepareAsync(GameMasterSnapshot snapshot, CancellationToken cancellationToken)
    {
        var packId = _packId
            ?? snapshot.PackCatalog?.Packs.FirstOrDefault(pack => pack.IsValid)?.Id
            ?? throw new BotException("Aucun pack valide sur le serveur.");
        if (snapshot.SelectedPackId != packId)
        {
            var selected = await _connection
                .InvokeAsync<SelectPackResult>("SelectPack", new SelectPackRequest(packId), cancellationToken)
                .ConfigureAwait(false);
            if (selected.Refusal is { } refusal)
            {
                throw new BotException($"Pack « {packId} » refusé ({refusal}).");
            }

            return;
        }

        if (snapshot.Players.Count(player => player.IsConnected) >= Math.Max(_playerCount, snapshot.MinimumPlayerCount))
        {
            await _connection.InvokeAsync<StartGameResult>("StartGame", cancellationToken).ConfigureAwait(false);
        }
    }

    private void Receive(GameMasterSnapshot snapshot)
    {
        lock (_gate)
        {
            // One of another game is newer whatever its version: a new game was started.
            if (_snapshot is not null && snapshot.GameId == _snapshot.GameId && snapshot.Version <= _snapshot.Version)
            {
                return;
            }

            _snapshot = snapshot;
        }

        _wakeUps.Writer.TryWrite(false);
    }
}
