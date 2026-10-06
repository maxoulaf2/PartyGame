using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

public sealed class RecoverSessionTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public RecoverSessionTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory.Services.GetRequiredService<GameLoop>();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
    }

    [Fact]
    public async Task JoinGame_NewPlayers_ShowsEachADistinctReconnectionCodeToTheGameMasterOnly()
    {
        // Given
        await using var gameMaster = await HubClients.ConnectAsync(_factory);
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toZoe = new ReceivedSnapshots(zoe);
        using var toMax = new ReceivedSnapshots(max);
        await AnnounceAsync(gameMaster, Role.GameMaster);
        await AnnounceAsync(display, Role.Display);

        // When
        await zoe.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);
        await max.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Max"), Ct);

        // Then
        await Task.WhenAll(FlushAsync(gameMaster), FlushAsync(display), FlushAsync(zoe), FlushAsync(max));
        var codes = toGameMaster.GameMaster.MaxBy(s => s.Version)!.Players.Select(p => p.ReconnectionCode!).ToList();
        Assert.Equal(2, codes.Distinct().Count());
        Assert.All(codes, code => Assert.Matches("^[A-HJKMNP-Z2-9]{6}$", code));
        var secrets = codes.Select(code => new Secret(code, Audience.AllButGameMaster)).ToArray();
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Json, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Max"), toMax.Json, secrets);
    }

    [Fact]
    public async Task RecoverSession_KnownCodeTypedLoosely_GivesTheTokenThatResumesTheSession()
    {
        // Given: Zoé lost her phone
        await using var lost = await HubClients.ConnectAsync(_factory);
        var joined = await lost.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);
        var code = Game.State.ReconnectionCodes[joined.PlayerId!.Value];
        await using var phone = await HubClients.ConnectAsync(_factory);

        // When
        var recovered = await RecoverAsync(phone, $"  {code.ToLowerInvariant()} ");
        var resumed = await phone.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, new ResumeSessionRequest(recovered.Token!), Ct);

        // Then
        Assert.Equal(new RecoverSessionResult(Refusal: null, joined.Token, LastClientSeq: 0), recovered);
        Assert.Equal(new ResumeSessionResult(Refusal: null, joined.PlayerId), resumed);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("recovered their session", StringComparison.Ordinal));
        Assert.DoesNotContain(code, _logs.ReadAllLogs(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ZZZZZZ")]
    [InlineData("")]
    public async Task RecoverSession_UnknownCode_IsRefusedAndLogsWarningWithoutTheCode(string code)
    {
        // Given
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await zoe.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);
        await using var stranger = await HubClients.ConnectAsync(_factory);

        // When
        var result = await RecoverAsync(stranger, code);

        // Then
        Assert.Equal(new RecoverSessionResult(RecoverSessionRefusal.CodeUnknown, Token: null, LastClientSeq: 0), result);
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("unknown reconnection code", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
        if (code.Length > 0)
        {
            Assert.DoesNotContain(code, _logs.ReadAllLogs(), StringComparison.Ordinal);
        }
    }

    public static TheoryData<string, string> MalformedRequests => new()
    {
        { "{}", "$" },
        { """{ "code": null }""", "$.code" },
        { """{ "code": 42 }""", "$.code" },
        { "null", "$" },
    };

    [Theory]
    [MemberData(nameof(MalformedRequests))]
    public async Task RecoverSession_MalformedMessage_IsRefusedAndLogsWarning(string message, string expectedPath)
    {
        // Given
        await using var phone = await HubClients.ConnectAsync(_factory);
        using var json = JsonDocument.Parse(message);

        // When
        var result = await phone.InvokeAsync<RecoverSessionResult>(GameHub.RecoverSession, json.RootElement, Ct);

        // Then
        Assert.Equal(RecoverSessionRefusal.MessageInvalid, result.Refusal);
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Malformed", StringComparison.Ordinal));
        Assert.Contains($"\"JsonPath\":\"{expectedPath}\"", warning.Line, StringComparison.Ordinal);
    }

    private static async Task AnnounceAsync(HubConnection connection, Role role)
    {
        var result = await connection.InvokeAsync<AnnouncementResult>(
            GameHub.Announce, new Announcement(role, role == Role.GameMaster ? Code : null), Ct);
        Assert.Null(result.Refusal);
    }

    private static Task<RecoverSessionResult> RecoverAsync(HubConnection connection, string code) =>
        connection.InvokeAsync<RecoverSessionResult>(GameHub.RecoverSession, new RecoverSessionRequest(code), Ct);

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
