using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class ShowJoinCodeTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public ShowJoinCodeTests() =>
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseScratchDirectory(_logs.Path).UseSetting("GameMaster:Code", Code));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
    }

    [Fact]
    public async Task ShowJoinCode_GameMaster_ShowsTheCodeOnTheTvAndTheConsole()
    {
        // Given
        await using var display = await ConnectAsync(Role.Display);
        await using var gameMaster = await ConnectAsync(Role.GameMaster, Code);
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When
        await gameMaster.InvokeAsync(GameHub.ShowJoinCode, new ShowJoinCodeRequest(Shown: true), Ct);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster));
        Assert.True(Assert.Single(toDisplay.Display).JoinCodeShown);
        Assert.True(Assert.Single(toGameMaster.GameMaster).JoinCodeShown);
    }

    [Fact]
    public async Task ShowJoinCode_NotAuthenticatedAsGameMaster_IsIgnored()
    {
        // Given
        await using var display = await ConnectAsync(Role.Display);
        var state = _factory.Services.GetRequiredService<GameLoop>().State;

        // When
        await display.InvokeAsync(GameHub.ShowJoinCode, new ShowJoinCodeRequest(Shown: true), Ct);

        // Then
        Assert.Same(state, _factory.Services.GetRequiredService<GameLoop>().State);
    }

    private async Task<HubConnection> ConnectAsync(Role role, string? code = null)
    {
        var connection = await HubClients.ConnectAsync(_factory);
        var result = await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(role, code), Ct);
        Assert.Null(result.Refusal);
        return connection;
    }

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
