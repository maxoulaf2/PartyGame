using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class PreviewTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public PreviewTests()
    {
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Grande soirée", "Échauffement", "Finale"));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
        _packs.Dispose();
    }

    [Fact]
    public async Task Preview_GameMaster_DrivesTheTvScreen()
    {
        // Given
        await using var display = await ConnectAsync(Role.Display);
        await using var gameMaster = await ConnectAsync(Role.GameMaster, Code);
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster));
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When
        await gameMaster.InvokeAsync(GameHub.StartPreview, new StartPreviewRequest("soiree"), Ct);
        await gameMaster.InvokeAsync(GameHub.ShowPreviewStep, new ShowPreviewStepRequest(RoundNumber: 2, StepNumber: 1, PlayExcerpt: false), Ct);
        await gameMaster.InvokeAsync(GameHub.StopPreview, Ct);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster));
        Assert.Collection(
            toDisplay.Display,
            first => Assert.Equal(("Échauffement", QuizQuestionPhase.Revealed), (first.Preview!.Round.Title, Assert.IsType<QuizDisplayView>(first.Preview.View).Phase)),
            second => Assert.Equal("Finale", second.Preview!.Round.Title),
            last => Assert.Null(last.Preview));
        Assert.Equal("soiree", toGameMaster.GameMaster[0].Preview!.PackId);
    }

    [Fact]
    public async Task StartPreview_NotAuthenticatedAsGameMaster_IsIgnored()
    {
        // Given
        await using var display = await ConnectAsync(Role.Display);
        var state = _factory.Services.GetRequiredService<GameLoop>().State;

        // When
        await display.InvokeAsync(GameHub.StartPreview, new StartPreviewRequest("soiree"), Ct);

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
