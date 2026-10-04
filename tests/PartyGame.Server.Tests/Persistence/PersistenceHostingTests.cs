using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Persistence;
using PartyGame.Server.Tests.Hubs;
using PartyGame.Server.Tests.Packs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Persistence;

public sealed class PersistenceHostingTests : IDisposable
{
    private const string Code = "571346";

    private static readonly TimeSpan _startupTimeout = TimeSpan.FromSeconds(30);

    private readonly TempDirectory _logs = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _logs.Dispose();

    [Fact]
    public async Task Shutdown_AfterAPlayerJoined_TheSaveHoldsTheGameWithItsSecretsButNeverTheGameMasterCode()
    {
        // Given
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code));
        GameLoop loop;
        string token;
        await using (factory)
        {
            loop = factory.Services.GetRequiredService<GameLoop>();
            await using var phone = await HubClients.ConnectAsync(factory);
            var joined = await phone.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);
            token = joined.Token!;

            // When: the server stops normally, the phone leaving with it
        }

        // Then: its last state is saved, down to the phone that left
        var saved = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(_logs.Path, GamePersistence.FileName), Ct))!;
        var game = saved["game"]!;
        Assert.Equal(loop.State.Version, game["version"]!.GetValue<long>());
        Assert.False(game["players"]![0]!["isConnected"]!.GetValue<bool>());
        Assert.Equal("Zoé", game["players"]![0]!["nickname"]!.GetValue<string>());
        Assert.NotNull(game["playerTokens"]![token]);
        Assert.Empty(Leaks.SecretsShown(Viewer.GameMaster, saved, [new Secret(Code, Audience.Everyone)]));
    }

    [Fact]
    public async Task SendRoundIntent_Acknowledged_TheAnswerIsSavedAlready()
    {
        // Given: Zoé and Max play the only pack of the directory, and its answers are open
        using var packs = new TempDirectory();
        TestPacks.Write(packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Échauffement"));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, packs.Path));
        var loop = factory.Services.GetRequiredService<GameLoop>();
        await using var zoe = await HubClients.ConnectAsync(factory);
        await zoe.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);
        await using var max = await HubClients.ConnectAsync(factory);
        await max.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Max"), Ct);
        await using var gameMaster = await HubClients.ConnectAsync(factory);
        await gameMaster.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(Role.GameMaster, Code), Ct);
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        var round = loop.State.CurrentRound!.Id;
        GameMasterRoundIntent[] steps = [new QuizShowQuestion(round, 1), new QuizShowChoice(round, 1, QuizChoiceLetter.A)];
        foreach (var step in steps)
        {
            await gameMaster.InvokeAsync(GameHub.SendGameMasterRoundIntent, JsonSerializer.SerializeToElement(step, ContractJsonOptions.Default), Ct);
        }

        // When: each answer is acknowledged
        foreach (var phone in new[] { zoe, max })
        {
            await PlayerIntents.SendAsync(phone, clientSeq: 1, new QuizSubmitAnswer(round, 1, QuizChoiceLetter.A));

            // Then: a server killed now would find it in its save, the phone never sending it again
            Assert.Equal(loop.State.Version, SavedVersion());
        }
    }

    [Fact]
    public async Task Startup_DataDirectoryUnusable_ExitsNamingTheFolderAndTheSetting()
    {
        // Given: a file stands where the folder should be
        var file = Path.Combine(_logs.Path, "fichier");
        Directory.CreateDirectory(_logs.Path);
        await File.WriteAllTextAsync(file, string.Empty, Ct);
        var directory = Path.Combine(file, "data");

        // When
        using var server = ServerProcess.Start(new Dictionary<string, string>
        {
            ["Network__Port"] = ServerProcess.GetFreePort().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["LogFiles__Directory"] = _logs.Path,
            ["Persistence__Directory"] = directory,
        });
        var exitCode = await server.WaitForExitAsync(_startupTimeout);

        // Then
        Assert.Equal(1, exitCode);
        Assert.Contains($"Data directory {directory} cannot be used", server.Output, StringComparison.Ordinal);
        Assert.Contains(PersistenceOptions.DirectorySetting, server.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", server.Output, StringComparison.Ordinal);
    }

    // Shared for deletion too: the writer may replace the file while the test reads it.
    private long SavedVersion()
    {
        using var stream = new FileStream(Path.Combine(_logs.Path, GamePersistence.FileName), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return JsonNode.Parse(stream)!["game"]!["version"]!.GetValue<long>();
    }
}
