using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Network;
using PartyGame.Server.Tests.Network;
using PartyGame.Tests.Shared.Leaks;
using static PartyGame.Server.Tests.Network.TestInterfaces;

namespace PartyGame.Server.Tests.Hubs;

public sealed class ChooseAdvertisedAddressTests : IAsyncDisposable
{
    private const string Code = "482913";
    private const string WifiAddress = "192.168.1.42";
    private const string EthernetAddress = "10.0.0.2";

    private readonly TempDirectory _logs = new();
    private readonly List<WebApplicationFactory<Program>> _factories = [];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        foreach (var factory in _factories)
        {
            await factory.DisposeAsync();
        }

        _logs.Dispose();
    }

    [Fact]
    public async Task Announce_SeveralCandidates_GivesThemToTheGameMasterOnly()
    {
        // Given
        var factory = CreateFactory();
        await using var display = await HubClients.ConnectAsync(factory);
        await using var gameMaster = await HubClients.ConnectAsync(factory);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When
        await AnnounceAsync(display, Role.Display);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);

        // Then
        await Task.WhenAll(FlushAsync(factory, display), FlushAsync(factory, gameMaster));
        var snapshot = Assert.Single(toGameMaster.GameMaster);
        Assert.Equal(WifiAddress, snapshot.JoinAddress);
        Assert.Equal(
            [new GameMasterJoinAddress(WifiAddress, "Wi-Fi"), new GameMasterJoinAddress(EthernetAddress, "Ethernet")],
            snapshot.JoinAddressCandidates);
        Assert.Equal(WifiAddress, Assert.Single(toDisplay.Display).JoinAddress);
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, new Secret(EthernetAddress, Audience.AllButGameMaster));
    }

    [Fact]
    public async Task ChooseAdvertisedAddress_Candidate_ChangesTheAddressOnTheTvAndTheConsole()
    {
        // Given
        var factory = CreateFactory();
        await using var display = await HubClients.ConnectAsync(factory);
        await using var gameMaster = await ConnectGameMasterAsync(factory);
        await AnnounceAsync(display, Role.Display);
        await Task.WhenAll(FlushAsync(factory, display), FlushAsync(factory, gameMaster));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When
        var result = await ChooseAsync(gameMaster, EthernetAddress);

        // Then
        Assert.Equal(new ChooseAdvertisedAddressResult(Refusal: null), result);
        Assert.Equal(EthernetAddress, GameOf(factory).State.JoinAddress);
        await Task.WhenAll(FlushAsync(factory, display), FlushAsync(factory, gameMaster));
        Assert.Equal(EthernetAddress, Assert.Single(toDisplay.Display).JoinAddress);
        Assert.Equal(EthernetAddress, Assert.Single(toGameMaster.GameMaster).JoinAddress);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("chosen by the game master", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("192.168.1.99")]
    [InlineData("example.com")]
    [InlineData("")]
    public async Task ChooseAdvertisedAddress_NoCandidate_IsRefusedAndBroadcastsNothing(string address)
    {
        // Given
        var factory = CreateFactory();
        await using var gameMaster = await ConnectGameMasterAsync(factory);
        await FlushAsync(factory, gameMaster);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        var state = GameOf(factory).State;

        // When
        var result = await ChooseAsync(gameMaster, address);

        // Then
        Assert.Equal(new ChooseAdvertisedAddressResult(ChooseAdvertisedAddressRefusal.AddressUnknown), result);
        Assert.Same(state, GameOf(factory).State);
        await FlushAsync(factory, gameMaster);
        Assert.Empty(toGameMaster.Json);
    }

    [Fact]
    public async Task ChooseAdvertisedAddress_AddressConfigured_IsTheCurrentChoiceAndCanBeReplacedThenChosenBack()
    {
        // Given
        var factory = CreateFactory(advertisedAddress: "192.168.50.7");
        await using var gameMaster = await HubClients.ConnectAsync(factory);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);
        await FlushAsync(factory, gameMaster);
        var initial = Assert.Single(toGameMaster.GameMaster);

        // When
        var replaced = await ChooseAsync(gameMaster, WifiAddress);
        var chosenBack = await ChooseAsync(gameMaster, "192.168.50.7");

        // Then
        Assert.Equal("192.168.50.7", initial.JoinAddress);
        Assert.Equal(new GameMasterJoinAddress("192.168.50.7", InterfaceName: null), initial.JoinAddressCandidates[0]);
        Assert.Null(replaced?.Refusal);
        Assert.Null(chosenBack?.Refusal);
        Assert.Equal("192.168.50.7", GameOf(factory).State.JoinAddress);
    }

    [Fact]
    public async Task ChooseAdvertisedAddress_ThenRestart_ForgetsTheChoice()
    {
        // Given
        var first = CreateFactory();
        await using (var gameMaster = await ConnectGameMasterAsync(first))
        {
            Assert.Null((await ChooseAsync(gameMaster, EthernetAddress))?.Refusal);
        }

        _factories.Remove(first);
        await first.DisposeAsync();

        // When
        var restarted = CreateFactory();

        // Then
        Assert.Equal(WifiAddress, GameOf(restarted).State.JoinAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Role.Display)]
    public async Task ChooseAdvertisedAddress_NotAuthenticatedAsGameMaster_IsIgnored(Role? role)
    {
        // Given
        var factory = CreateFactory();
        await using var connection = await HubClients.ConnectAsync(factory);
        if (role is { } announced)
        {
            await AnnounceAsync(connection, announced);
        }

        var state = GameOf(factory).State;

        // When
        var result = await ChooseAsync(connection, EthernetAddress);

        // Then
        Assert.Null(result);
        Assert.Same(state, GameOf(factory).State);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("not authenticated as game master", StringComparison.Ordinal));
    }

    public static TheoryData<string, string> MalformedRequests => new()
    {
        { "{}", "$" },
        { """{ "address": 42 }""", "$.address" },
        { """{ "address": null }""", "$.address" },
        { "null", "$" },
    };

    [Theory]
    [MemberData(nameof(MalformedRequests))]
    public async Task ChooseAdvertisedAddress_MalformedMessage_IsRefusedAndLogsWarning(string message, string expectedPath)
    {
        // Given
        var factory = CreateFactory();
        await using var gameMaster = await ConnectGameMasterAsync(factory);
        using var json = JsonDocument.Parse(message);
        var state = GameOf(factory).State;

        // When
        var result = await gameMaster.InvokeAsync<ChooseAdvertisedAddressResult>(GameHub.ChooseAdvertisedAddress, json.RootElement, Ct);

        // Then
        Assert.Equal(new ChooseAdvertisedAddressResult(ChooseAdvertisedAddressRefusal.MessageInvalid), result);
        Assert.Same(state, GameOf(factory).State);
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Malformed", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
        Assert.Contains($"\"JsonPath\":\"{expectedPath}\"", warning.Line, StringComparison.Ordinal);
    }

    /// <summary>A host on the Wi-Fi of the venue and on a wired network without gateway.</summary>
    private WebApplicationFactory<Program> CreateFactory(string? advertisedAddress = null)
    {
        var source = new FakeNetworkInterfaceSource(Wifi(WifiAddress), Ethernet(EthernetAddress, hasGateway: false));
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseScratchDirectory(_logs.Path).UseSetting("GameMaster:Code", Code);
            if (advertisedAddress is not null)
            {
                builder.UseSetting("Network:AdvertisedAddress", advertisedAddress);
            }

            builder.ConfigureTestServices(services => services.AddSingleton<INetworkInterfaceSource>(source));
        });
        _factories.Add(factory);
        return factory;
    }

    private static GameLoop GameOf(WebApplicationFactory<Program> factory) => factory.Services.GetRequiredService<GameLoop>();

    private static async Task<HubConnection> ConnectGameMasterAsync(WebApplicationFactory<Program> factory)
    {
        var connection = await HubClients.ConnectAsync(factory);
        await AnnounceAsync(connection, Role.GameMaster, Code);
        return connection;
    }

    private static async Task AnnounceAsync(HubConnection connection, Role role, string? code = null)
    {
        var result = await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(role, code), Ct);
        Assert.Null(result.Refusal);
    }

    /// <summary>
    /// Chooses the address through the hub. The answer is <see langword="null"/> when the hub ignores the intent of a
    /// connection that is not authenticated as game master.
    /// </summary>
    private static Task<ChooseAdvertisedAddressResult?> ChooseAsync(HubConnection connection, string address) =>
        connection.InvokeAsync<ChooseAdvertisedAddressResult?>(
            GameHub.ChooseAdvertisedAddress,
            new ChooseAdvertisedAddressRequest(address),
            Ct);

    private static Task FlushAsync(WebApplicationFactory<Program> factory, HubConnection connection) =>
        HubClients.FlushAsync<GameHub>(factory, connection);
}
