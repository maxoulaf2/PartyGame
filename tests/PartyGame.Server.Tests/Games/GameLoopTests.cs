using Microsoft.Extensions.Logging;
using PartyGame.Engine;
using PartyGame.Engine.Effects;
using PartyGame.Server.Games;

namespace PartyGame.Server.Tests.Games;

public sealed class GameLoopTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WriteAsync_InputsFromParallelThreads_AreHandledOneAtATimeInQueueOrder()
    {
        // Given
        const int Producers = 8;
        const int InputsPerProducer = 50;
        await using var harness = await LoopHarness.StartAsync();

        // When
        await Task.WhenAll(Enumerable.Range(0, Producers).Select(producer => Task.Run(
            async () =>
            {
                for (var i = 0; i < InputsPerProducer; i++)
                {
                    await harness.Loop.WriteAsync(new TestInput((producer * 1000) + i), Ct);
                }
            },
            Ct)));
        await harness.Loop.SubmitAsync(new TestInput(-1), Ct);

        // Then
        Assert.Equal(1, harness.Engine.MaxConcurrentCalls);
        var handled = harness.Loop.State.Players.Select(p => int.Parse(p.Nickname, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        Assert.Equal((Producers * InputsPerProducer) + 1, handled.Count);
        for (var producer = 0; producer < Producers; producer++)
        {
            var fromProducer = handled.Where(v => v >= producer * 1000 && v < (producer + 1) * 1000).ToList();
            Assert.Equal(Enumerable.Range(producer * 1000, InputsPerProducer), fromProducer);
        }
    }

    [Fact]
    public async Task SubmitAsync_AcceptedInput_ReplacesStateThenExecutesEffectsThenNotifies()
    {
        // Given
        Effect[] effects = [new ScheduleTimer(new TimerId("a"), DateTimeOffset.UnixEpoch), new CancelTimer(new TimerId("b"))];
        var engine = new ScriptedEngine((state, input, context) =>
            ScriptedEngine.AddPlayer(state, input, context) with { Effects = [.. effects] });
        await using var harness = await LoopHarness.StartAsync(engine);

        // When
        var outcome = await harness.Loop.SubmitAsync(new TestInput(1), Ct);

        // Then
        Assert.Equal(InputOutcome.Accepted, outcome);
        var newState = harness.Loop.State;
        Assert.NotSame(LoopHarness.InitialState, newState);
        Assert.Equal([$"effect:{effects[0]}", $"effect:{effects[1]}", "notify:listener"], harness.Journal.Entries);
        Assert.Same(newState, Assert.Single(harness.Listeners[0].States));
    }

    [Fact]
    public async Task SubmitAsync_RejectedInput_NotifiesNothingAndLogsDebug()
    {
        // Given
        var engine = new ScriptedEngine((state, _, _) => Transition.Rejected(state, RejectionReason.NicknameTaken));
        await using var harness = await LoopHarness.StartAsync(engine);

        // When
        var outcome = await harness.Loop.SubmitAsync(new TestInput(1), Ct);

        // Then
        Assert.Equal(InputOutcome.Rejected(RejectionReason.NicknameTaken), outcome);
        Assert.Same(LoopHarness.InitialState, harness.Loop.State);
        Assert.Empty(harness.Journal.Entries);
        var entry = Assert.Single(harness.Logger.Entries, e => e.Message.Contains("rejected", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains(nameof(TestInput), entry.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(RejectionReason.NicknameTaken), entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitAsync_EngineThrows_KeepsStateLogsErrorAndHandlesNextInputs()
    {
        // Given
        var engine = new ScriptedEngine((state, input, context) => ((TestInput)input).Value == 1
            ? throw new InvalidOperationException("Injected engine failure")
            : ScriptedEngine.AddPlayer(state, input, context));
        await using var harness = await LoopHarness.StartAsync(engine);

        // When
        var failed = await harness.Loop.SubmitAsync(new TestInput(1), Ct);
        var stateAfterFailure = harness.Loop.State;
        var next = await harness.Loop.SubmitAsync(new TestInput(2), Ct);

        // Then
        Assert.Equal(InputOutcome.Failed, failed);
        Assert.Same(LoopHarness.InitialState, stateAfterFailure);
        var error = Assert.Single(harness.Logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains(nameof(TestInput), error.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(error.Exception);
        Assert.Equal(InputOutcome.Accepted, next);
        Assert.Equal("2", Assert.Single(harness.Loop.State.Players).Nickname);
        Assert.Equal(["notify:listener"], harness.Journal.Entries);
    }

    [Fact]
    public async Task SubmitAsync_EffectThrows_ExecutesOtherEffectsAndNotifies()
    {
        // Given
        var failing = new CancelTimer(new TimerId("failing"));
        var other = new CancelTimer(new TimerId("other"));
        var engine = new ScriptedEngine((state, input, context) =>
            ScriptedEngine.AddPlayer(state, input, context) with { Effects = [failing, other] });
        await using var harness = await LoopHarness.StartAsync(engine, effectFails: effect => effect == failing);

        // When
        var outcome = await harness.Loop.SubmitAsync(new TestInput(1), Ct);

        // Then
        Assert.Equal(InputOutcome.Accepted, outcome);
        Assert.Equal([$"effect:{other}", "notify:listener"], harness.Journal.Entries);
        var error = Assert.Single(harness.Logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains(nameof(CancelTimer), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitAsync_ListenerThrows_NotifiesOtherListeners()
    {
        // Given
        await using var harness = await LoopHarness.StartAsync(
            listeners: [journal => new RecordingListener(journal, "failing", fails: true), journal => new RecordingListener(journal, "other")]);

        // When
        var outcome = await harness.Loop.SubmitAsync(new TestInput(1), Ct);

        // Then
        Assert.Equal(InputOutcome.Accepted, outcome);
        Assert.Equal(["notify:other"], harness.Journal.Entries);
        Assert.Single(harness.Logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task SubmitAsync_AnyInput_GivesEngineTheTimeOfTheInjectedTimeProvider()
    {
        // Given
        await using var harness = await LoopHarness.StartAsync();
        harness.Time.Advance(TimeSpan.FromMinutes(5));

        // When
        await harness.Loop.SubmitAsync(new TestInput(1), Ct);

        // Then
        Assert.Equal(harness.Time.GetUtcNow(), harness.Engine.LastContext?.Now);
    }

    [Fact]
    public async Task StopAsync_RunningLoop_StopsCleanlyAndClosesQueue()
    {
        // Given
        var harness = await LoopHarness.StartAsync();
        await harness.Loop.SubmitAsync(new TestInput(1), Ct);

        // When
        await harness.DisposeAsync();

        // Then
        Assert.True(harness.Loop.ExecuteTask?.IsCompletedSuccessfully);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.Loop.WriteAsync(new TestInput(2), Ct).AsTask());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.Loop.SubmitAsync(new TestInput(3), Ct));
    }

    [Fact]
    public async Task StopAsync_InputsStillQueued_CancelsTheirSubmitters()
    {
        // Given
        using var engineEntered = new ManualResetEventSlim();
        using var releaseEngine = new ManualResetEventSlim();
        var engine = new ScriptedEngine((state, input, context) =>
        {
            engineEntered.Set();
            releaseEngine.Wait(Ct);
            return ScriptedEngine.AddPlayer(state, input, context);
        });
        var harness = await LoopHarness.StartAsync(engine);
        var inProgress = harness.Loop.SubmitAsync(new TestInput(1), Ct);
        engineEntered.Wait(Ct);
        var queued = harness.Loop.SubmitAsync(new TestInput(2), Ct);

        // When
        var stopping = harness.DisposeAsync();
        releaseEngine.Set();
        await stopping;

        // Then
        Assert.Equal(InputOutcome.Accepted, await inProgress);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        Assert.True(harness.Loop.ExecuteTask?.IsCompletedSuccessfully);
    }
}
