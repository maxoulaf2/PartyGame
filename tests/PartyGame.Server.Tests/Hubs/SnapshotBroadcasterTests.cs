using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Projections;
using PartyGame.Engine.State;
using PartyGame.Server.Hubs;
using PartyGame.Server.Tests.Games;

namespace PartyGame.Server.Tests.Hubs;

public sealed class SnapshotBroadcasterTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OnStateChangedAsync_OneGroupFails_LogsWarningAndSendsToTheOthers()
    {
        // Given
        var hub = new RecordingHubContext(failingGroup: HubGroups.Display);
        var logger = new RecordingLogger<SnapshotBroadcaster>();
        var modes = new GameModes([]);
        var broadcaster = new SnapshotBroadcaster(hub, new Snapshots(modes), new RecordingIncidentReporter(), logger);
        var state = new GameEngine(modes).Handle(
            LoopHarness.InitialState,
            new JoinGame(new PlayerId(Guid.NewGuid()), new PlayerToken("token"), "Zoé", DateTimeOffset.UnixEpoch, "CODE01"),
            new GameContext(DateTimeOffset.UnixEpoch, new Random(42))).State;
        var player = Assert.Single(state.Players);

        // When
        await broadcaster.OnStateChangedAsync(state, Ct);

        // Then
        Assert.Equal([HubGroups.GameMaster, HubGroups.Player(player.Id)], hub.Sent);
        var warning = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Contains(HubGroups.Display, warning.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(warning.Exception);
    }

    [Theory]
    [InlineData(Role.Display, HubGroups.GameMaster)]
    [InlineData(Role.GameMaster, HubGroups.Display)]
    public async Task OnStateChangedAsync_ProjectionOfARoleThrows_SendsTheOthersTheirsAndReportsTheRole(Role failing, string other)
    {
        // Given
        var hub = new RecordingHubContext();
        var incidents = new RecordingIncidentReporter();
        var logger = new RecordingLogger<SnapshotBroadcaster>();
        var broadcaster = new SnapshotBroadcaster(hub, new Snapshots(new GameModes([new FaultyQuizMode(failing)])), incidents, logger);
        var state = RoundWith("Zoé");

        // When
        await broadcaster.OnStateChangedAsync(state, Ct);

        // Then
        Assert.Equal([other, HubGroups.Player(state.Players[0].Id)], hub.Sent);
        var incident = Assert.Single(incidents.Reported);
        Assert.Equal((IncidentCode.ProjectionFailed, failing), (incident.Code, incident.Role));
        Assert.Same(state, incident.State);
        var error = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, error.Level);
        Assert.Contains(failing.ToString(), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnStateChangedAsync_ProjectionOfPlayersThrows_SendsTheOthersTheirsAndReportsOnce()
    {
        // Given
        var hub = new RecordingHubContext();
        var incidents = new RecordingIncidentReporter();
        var logger = new RecordingLogger<SnapshotBroadcaster>();
        var broadcaster = new SnapshotBroadcaster(hub, new Snapshots(new GameModes([new FaultyQuizMode(Role.Player)])), incidents, logger);

        // When
        await broadcaster.OnStateChangedAsync(RoundWith("Zoé", "Max"), Ct);

        // Then
        Assert.Equal([HubGroups.Display, HubGroups.GameMaster], hub.Sent);
        var incident = Assert.Single(incidents.Reported);
        Assert.Equal((IncidentCode.ProjectionFailed, Role.Player), (incident.Code, incident.Role));
        Assert.Equal(2, logger.Entries.Count(e => e.Level == LogLevel.Error));
    }

    /// <summary>
    /// A game in its first round, a quiz round played by <see cref="FaultyQuizMode"/>, with the given players.
    /// </summary>
    private static GameState RoundWith(params string[] nicknames)
    {
        var descriptor = new QuizRoundDescriptor
        {
            Title = "Échauffement",
            Questions = [new QuizQuestion { Text = "Question ?", Choices = [new QuizChoice { Text = "Oui", Correct = true }, new QuizChoice { Text = "Non" }] }],
        };
        var state = LoopHarness.InitialState with
        {
            Phase = GamePhase.Round,
            Players = [.. nicknames.Select(nickname => new Player(new PlayerId(Guid.NewGuid()), nickname, IsConnected: true))],
            Pack = new PackDescriptor { FormatVersion = PackDescriptor.CurrentFormatVersion, Title = "Soirée", Rounds = [descriptor] },
        };
        var round = new FaultyQuizMode().Start(descriptor, state, new GameContext(DateTimeOffset.UnixEpoch, new Random(42))).State;
        return state with { CurrentRound = new PlayedRound(new RoundId(Guid.NewGuid()), Index: 0, round) };
    }

    /// <summary>
    /// Records the groups that received a snapshot, and fails to send to one of them, if any.
    /// </summary>
    private sealed class RecordingHubContext(string? failingGroup = null) : IHubContext<GameHub, IGameClient>
    {
        public List<string> Sent { get; } = [];

        public IHubClients<IGameClient> Clients => new GroupClients(this);

        public IGroupManager Groups => throw new NotSupportedException();

        private string? FailingGroup => failingGroup;

        private sealed class GroupClients(RecordingHubContext hub) : IHubClients<IGameClient>
        {
            public IGameClient All => throw new NotSupportedException();

            public IGameClient Group(string groupName) => new GroupClient(hub, groupName);

            public IGameClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();

            public IGameClient Client(string connectionId) => throw new NotSupportedException();

            public IGameClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();

            public IGameClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();

            public IGameClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();

            public IGameClient User(string userId) => throw new NotSupportedException();

            public IGameClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
        }

        private sealed class GroupClient(RecordingHubContext hub, string group) : IGameClient
        {
            public Task ReceiveWelcome(Welcome welcome) => throw new NotSupportedException();

            public Task ReceiveDisplaySnapshot(DisplaySnapshot snapshot) => Send();

            public Task ReceiveGameMasterSnapshot(GameMasterSnapshot snapshot) => Send();

            public Task ReceivePlayerSnapshot(PlayerSnapshot snapshot) => Send();

            public Task ReceiveIncidents(IncidentList incidents) => throw new NotSupportedException();

            public Task ReceiveNetworkHealth(NetworkHealth health) => throw new NotSupportedException();

            private Task Send()
            {
                if (group == hub.FailingGroup)
                {
                    throw new InvalidOperationException("Injected send failure");
                }

                hub.Sent.Add(group);
                return Task.CompletedTask;
            }
        }
    }
}
