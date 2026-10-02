using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Server.Hubs;
using PartyGame.Server.Tests.Games;

namespace PartyGame.Server.Tests.Hubs;

public sealed class SnapshotBroadcasterTests
{
    [Fact]
    public async Task OnStateChangedAsync_OneGroupFails_LogsWarningAndSendsToTheOthers()
    {
        // Given
        var hub = new RecordingHubContext(failingGroup: HubGroups.Display);
        var logger = new RecordingLogger<SnapshotBroadcaster>();
        var broadcaster = new SnapshotBroadcaster(hub, logger);
        var state = new GameEngine().Handle(
            LoopHarness.InitialState,
            new JoinGame(new PlayerId(Guid.NewGuid()), new PlayerToken("token"), "Zoé", DateTimeOffset.UnixEpoch),
            new GameContext(DateTimeOffset.UnixEpoch, new Random(42))).State;
        var player = Assert.Single(state.Players);

        // When
        await broadcaster.OnStateChangedAsync(state, TestContext.Current.CancellationToken);

        // Then
        Assert.Equal([HubGroups.GameMaster, HubGroups.Player(player.Id)], hub.Sent);
        var warning = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Contains(HubGroups.Display, warning.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(warning.Exception);
    }

    /// <summary>
    /// Records the groups that received a snapshot, and fails to send to one of them.
    /// </summary>
    private sealed class RecordingHubContext(string failingGroup) : IHubContext<GameHub, IGameClient>
    {
        public List<string> Sent { get; } = [];

        public IHubClients<IGameClient> Clients => new GroupClients(this);

        public IGroupManager Groups => throw new NotSupportedException();

        private string FailingGroup => failingGroup;

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
