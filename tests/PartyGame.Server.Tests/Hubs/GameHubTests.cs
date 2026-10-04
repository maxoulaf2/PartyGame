using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using PartyGame.Contracts;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class GameHubTests : IAsyncDisposable
{
    private const string Code = "482913";
    private const string WrongCode = "135792";

    private static readonly string[] _roleGroups = [HubGroups.Display, HubGroups.GameMaster];

    private readonly TempDirectory _logs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public GameHubTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code));
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
    }

    [Fact]
    public async Task Connect_WebSocketsOnly_Succeeds()
    {
        await using var connection = await HubClients.ConnectAsync(_factory);

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task Announce_Display_JoinsDisplayGroupOnly()
    {
        await using var connection = await HubClients.ConnectAsync(_factory);

        var result = await AnnounceAsync(connection, new Announcement(Role.Display, GameMasterCode: null));

        Assert.Null(result.Refusal);
        Assert.Equal([HubGroups.Display], await GroupsOfAsync(connection));
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Connection {ConnectionId} announced", StringComparison.Ordinal)
            && e.Line.Contains("\"Role\":\"Display\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(Code)]
    [InlineData($" {Code} ")]
    public async Task Announce_GameMasterWithRightCode_JoinsGameMasterGroupOnly(string code)
    {
        await using var connection = await HubClients.ConnectAsync(_factory);

        var result = await AnnounceAsync(connection, new Announcement(Role.GameMaster, code));

        Assert.Null(result.Refusal);
        Assert.Equal([HubGroups.GameMaster], await GroupsOfAsync(connection));
    }

    [Theory]
    [InlineData(WrongCode)]
    [InlineData("")]
    [InlineData(null)]
    public async Task Announce_GameMasterWithoutRightCode_IsRefusedAndJoinsNoGroup(string? code)
    {
        await using var connection = await HubClients.ConnectAsync(_factory);

        var result = await AnnounceAsync(connection, new Announcement(Role.GameMaster, code));

        Assert.Equal(AnnouncementRefusal.GameMasterCodeInvalid, result.Refusal);
        Assert.Empty(await GroupsOfAsync(connection));
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Level == "Warning" && e.Template.Contains("wrong game master code", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Announce_AgainWithWrongCode_LeavesPreviousGroup()
    {
        await using var connection = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(connection, new Announcement(Role.GameMaster, Code));

        var result = await AnnounceAsync(connection, new Announcement(Role.GameMaster, WrongCode));

        Assert.Equal(AnnouncementRefusal.GameMasterCodeInvalid, result.Refusal);
        Assert.Empty(await GroupsOfAsync(connection));
    }

    [Fact]
    public async Task Announce_AnotherRole_MovesToItsGroup()
    {
        await using var connection = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(connection, new Announcement(Role.GameMaster, Code));

        await AnnounceAsync(connection, new Announcement(Role.Display, GameMasterCode: null));

        Assert.Equal([HubGroups.Display], await GroupsOfAsync(connection));
    }

    public static TheoryData<string, string> MalformedAnnouncements => new()
    {
        { """{ "role": "Display" }""", "$" },
        { """{ "role": "Spectator", "gameMasterCode": null }""", "$.role" },
        { """{ "role": 1, "gameMasterCode": null }""", "$.role" },
        { """{ "role": null, "gameMasterCode": null }""", "$.role" },
        { """{ "role": "Player", "gameMasterCode": null }""", "$.role" },
        { """{ "role": "GameMaster", "gameMasterCode": 482913 }""", "$.gameMasterCode" },
        { "\"Display\"", "$" },
        { "null", "$" },
    };

    [Theory]
    [MemberData(nameof(MalformedAnnouncements))]
    public async Task Announce_MalformedMessage_IsIgnoredAndLogsWarning(string message, string expectedPath)
    {
        await using var connection = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(connection, new Announcement(Role.Display, GameMasterCode: null));

        using var json = JsonDocument.Parse(message);
        var result = await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, json.RootElement, TestContext.Current.CancellationToken);

        Assert.Equal(AnnouncementRefusal.MessageInvalid, result.Refusal);
        Assert.Equal([HubGroups.Display], await GroupsOfAsync(connection));
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Malformed", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
        Assert.Contains($"\"JsonPath\":\"{expectedPath}\"", warning.Line, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Announce_AnyCode_NeverLogsTheCode()
    {
        await using var connection = await HubClients.ConnectAsync(_factory);

        await AnnounceAsync(connection, new Announcement(Role.GameMaster, WrongCode));
        await AnnounceAsync(connection, new Announcement(Role.GameMaster, Code));
        using var malformed = JsonDocument.Parse($$"""{ "role": "Nobody", "gameMasterCode": "{{Code}}" }""");
        await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, malformed.RootElement, TestContext.Current.CancellationToken);

        var logs = _logs.ReadAllLogs();
        Assert.Contains("announced as", logs, StringComparison.Ordinal);
        Assert.DoesNotContain(Code, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(WrongCode, logs, StringComparison.Ordinal);
    }

    private static Task<AnnouncementResult> AnnounceAsync(HubConnection connection, Announcement announcement) =>
        connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, announcement, TestContext.Current.CancellationToken);

    private Task<IReadOnlyList<string>> GroupsOfAsync(HubConnection connection) =>
        HubClients.GroupsOfAsync<GameHub>(_factory, connection, _roleGroups);
}
