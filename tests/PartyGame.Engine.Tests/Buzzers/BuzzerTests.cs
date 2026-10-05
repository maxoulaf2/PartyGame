using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Buzzers;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests.Buzzers;

/// <summary>
/// The arbitration of the buzzer: the earliest press wins once the window after the first buzz ends, whatever the order
/// the buzzes arrive in, and no clock out of sync nor forged message can go back in time.
/// </summary>
public sealed class BuzzerTests
{
    private static readonly PlayerId _zoe = Games.PlayerIdOf(1);
    private static readonly PlayerId _max = Games.PlayerIdOf(2);
    private static readonly PlayerId _lea = Games.PlayerIdOf(3);

    private readonly FakeTimeProvider _time = new(Games.Now);

    /// <summary>The buzzer, opened at <see cref="Games.Now"/>.</summary>
    private readonly Buzzer _opened = new Buzzer().Open(Games.Now);

    [Fact]
    public void Buzz_FirstOfTheOpening_SchedulesTheArbitrationWindow()
    {
        // Given
        _time.Advance(TimeSpan.FromSeconds(2));

        // When
        var transition = Buzz(_opened, _zoe, PressedAgo(30));

        // Then
        Assert.Null(transition.Rejection);
        var arbitrateAt = _time.GetUtcNow().AddMilliseconds(250);
        Assert.Equal(arbitrateAt, transition.Buzzer.ArbitrateAt);
        Assert.Equal(new ScheduleTimer(Buzzer.ArbitrationTimer, arbitrateAt), Assert.Single(transition.Effects));
        Assert.Null(transition.Buzzer.Winner);
    }

    [Fact]
    public void Buzz_WindowOfTheContext_SetsTheArbitrationTime()
    {
        // When
        var transition = _opened.Buzz(_zoe, 1, Unix(Games.Now), Games.Now, Context() with { BuzzerArbitrationWindow = TimeSpan.Zero });

        // Then: no wait at all
        Assert.Equal(Games.Now, transition.Buzzer.ArbitrateAt);
    }

    [Fact]
    public void Buzz_DuringTheWindow_IsRetainedWithoutAnotherTimer()
    {
        // Given
        var buzzer = Accepted(Buzz(_opened, _zoe, PressedAgo(0)));
        _time.Advance(TimeSpan.FromMilliseconds(100));

        // When
        var transition = Buzz(buzzer, _max, PressedAgo(0));

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal([_zoe, _max], transition.Buzzer.Presses.Select(press => press.PlayerId));
        Assert.Equal(buzzer.ArbitrateAt, transition.Buzzer.ArbitrateAt);
    }

    [Fact]
    public void Arbitrate_EarlierPressArrivedSecond_WinsOverTheFirstArrived()
    {
        // Given: Zoé pressed 40 ms after Max, but her buzz arrived first
        _time.Advance(TimeSpan.FromSeconds(1));
        var pressedByMax = _time.GetUtcNow();
        _time.Advance(TimeSpan.FromMilliseconds(60));
        var buzzer = Accepted(Buzz(_opened, _zoe, pressedByMax.AddMilliseconds(40)));
        _time.Advance(TimeSpan.FromMilliseconds(60));
        buzzer = Accepted(Buzz(buzzer, _max, pressedByMax));

        // When
        var transition = Arbitrate(buzzer);

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(_max, transition.Buzzer.Winner);
        Assert.False(transition.Buzzer.IsOpen);
    }

    [Fact]
    public void Arbitrate_EqualPresses_FirstReceivedWins()
    {
        // Given
        _time.Advance(TimeSpan.FromSeconds(1));
        var pressedAt = _time.GetUtcNow();
        _time.Advance(TimeSpan.FromMilliseconds(20));
        var buzzer = Accepted(Buzz(_opened, _lea, pressedAt));
        _time.Advance(TimeSpan.FromMilliseconds(20));
        buzzer = Accepted(Buzz(buzzer, _zoe, pressedAt));

        // When
        var transition = Arbitrate(buzzer);

        // Then
        Assert.Equal(_lea, transition.Buzzer.Winner);
    }

    [Fact]
    public void Buzz_PressedAfterItsReception_CountsForItsReception()
    {
        // Given: Zoé's clock is 300 ms ahead, Max pressed 10 ms after Zoé's buzz arrived
        _time.Advance(TimeSpan.FromSeconds(1));
        var buzzer = Accepted(Buzz(_opened, _zoe, PressedAgo(-300)));
        _time.Advance(TimeSpan.FromMilliseconds(50));
        buzzer = Accepted(Buzz(buzzer, _max, PressedAgo(40)));

        // When
        var transition = Arbitrate(buzzer);

        // Then: her clock costs her nothing beyond the real arrival of her buzz
        Assert.Equal(buzzer.Presses[0].ReceivedAt, buzzer.Presses[0].PressedAt);
        Assert.Equal(_zoe, transition.Buzzer.Winner);
    }

    [Fact]
    public void Buzz_PressedBeforeTheOpening_IsBroughtBackToTheOpening()
    {
        // Given: Max pressed right at the opening, Zoé claims a press 500 ms before it, her buzz arriving second
        _time.Advance(TimeSpan.FromMilliseconds(200));
        var buzzer = Accepted(Buzz(_opened, _max, Games.Now));
        _time.Advance(TimeSpan.FromMilliseconds(10));
        buzzer = Accepted(Buzz(buzzer, _zoe, Games.Now.AddMilliseconds(-500)));

        // When
        var transition = Arbitrate(buzzer);

        // Then: her press gains nothing over the opening
        Assert.Equal(Games.Now, buzzer.Presses[1].PressedAt);
        Assert.Equal(_max, transition.Buzzer.Winner);
    }

    [Fact]
    public void Buzz_PressedMoreThanASecondBeforeItsReception_IsBroughtBackToASecondBefore()
    {
        // Given
        _time.Advance(TimeSpan.FromSeconds(10));

        // When
        var transition = Buzz(_opened, _zoe, PressedAgo(5_000));

        // Then
        Assert.Equal(_time.GetUtcNow().AddSeconds(-1), Accepted(transition).Presses[0].PressedAt);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void Buzz_ForgedTimeStamp_IsBroughtBackWithinBounds(long pressedAt)
    {
        // Given
        _time.Advance(TimeSpan.FromSeconds(10));
        var now = _time.GetUtcNow();

        // When
        var transition = _opened.Buzz(_zoe, 1, pressedAt, now, Context());

        // Then
        var press = Accepted(transition).Presses[0];
        Assert.InRange(press.PressedAt, now.AddSeconds(-1), now);
    }

    [Fact]
    public void Buzz_AfterTheWinnerIsDesignated_IsRejected()
    {
        // Given
        var buzzer = Arbitrate(Accepted(Buzz(_opened, _zoe, PressedAgo(0)))).Buzzer;

        // When
        var transition = Buzz(buzzer, _max, PressedAgo(0));

        // Then
        AssertRejected(buzzer, transition, RejectionReason.BuzzerClosed);
    }

    [Fact]
    public void Buzz_ReceivedAfterTheWindowBeforeItsTimer_IsRejected()
    {
        // Given: the loop has not handled the timer yet
        var buzzer = Accepted(Buzz(_opened, _zoe, PressedAgo(0)));
        _time.Advance(TimeSpan.FromMilliseconds(251));

        // When
        var transition = Buzz(buzzer, _max, PressedAgo(260));

        // Then
        AssertRejected(buzzer, transition, RejectionReason.BuzzerClosed);
    }

    [Fact]
    public void Buzz_BeforeTheFirstOpening_IsRejected()
    {
        // Given
        var buzzer = new Buzzer();

        // When
        var transition = buzzer.Buzz(_zoe, 0, Unix(Games.Now), Games.Now, Context());

        // Then
        AssertRejected(buzzer, transition, RejectionReason.BuzzerClosed);
    }

    [Fact]
    public void Buzz_BlockedPlayer_IsRejected()
    {
        // Given: Zoé won, answered wrong, and the buzzer reopened to the others
        var won = Arbitrate(Accepted(Buzz(_opened, _zoe, PressedAgo(0)))).Buzzer;
        _time.Advance(TimeSpan.FromSeconds(5));
        var reopened = won.Block(_zoe).Reopen(_time.GetUtcNow());

        // When
        var transition = Buzz(reopened, _zoe, PressedAgo(0), opening: 2);

        // Then
        AssertRejected(reopened, transition, RejectionReason.PlayerBlocked);
        Assert.Null(Buzz(reopened, _max, PressedAgo(0), opening: 2).Rejection);
    }

    [Fact]
    public void Open_BlockedPlayer_MayBuzzAgain()
    {
        // Given: Zoé is blocked on the previous question
        var blocked = Arbitrate(Accepted(Buzz(_opened, _zoe, PressedAgo(0)))).Buzzer.Block(_zoe);

        // When
        var transition = Buzz(blocked.Open(_time.GetUtcNow()), _zoe, PressedAgo(0), opening: 2);

        // Then
        Assert.Null(transition.Rejection);
    }

    [Fact]
    public void Buzz_SecondOfTheSamePlayer_IsRejected()
    {
        // Given
        var buzzer = Accepted(Buzz(_opened, _zoe, PressedAgo(0)));
        _time.Advance(TimeSpan.FromMilliseconds(50));

        // When: an earlier time stamp changes nothing
        var transition = Buzz(buzzer, _zoe, PressedAgo(200));

        // Then
        AssertRejected(buzzer, transition, RejectionReason.AlreadyBuzzed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Buzz_OtherOpening_IsRejectedAsObsolete(int opening)
    {
        // Given: Zoé's buzz of the first opening comes back after a reconnection, once the buzzer reopened
        var won = Arbitrate(Accepted(Buzz(_opened, _max, PressedAgo(0)))).Buzzer;
        var reopened = won.Block(_max).Reopen(_time.GetUtcNow());

        // When
        var transition = Buzz(reopened, _zoe, PressedAgo(0), opening);

        // Then
        AssertRejected(reopened, transition, RejectionReason.BuzzerOpeningMismatch);
    }

    [Fact]
    public void Arbitrate_TimerOfAnotherOpening_IsRejected()
    {
        // Given: the first opening was decided and the buzzer reopened before its old timer came back
        var first = Accepted(Buzz(_opened, _zoe, PressedAgo(0)));
        var reopened = Arbitrate(first).Buzzer.Block(_zoe).Reopen(_time.GetUtcNow());

        // When
        var transition = reopened.Arbitrate(new TimerElapsed(Buzzer.ArbitrationTimer, first.ArbitrateAt!.Value));

        // Then
        AssertRejected(reopened, transition, RejectionReason.UnexpectedTimer);
    }

    [Fact]
    public void Resume_SavedDuringTheWindow_KeepsTheBuzzesAndSchedulesWhatWasLeftOfIt()
    {
        // Given: saved 100 ms into the window, Max pressed before Zoé but his buzz arrived second
        _time.Advance(TimeSpan.FromSeconds(1));
        var pressedByMax = _time.GetUtcNow();
        _time.Advance(TimeSpan.FromMilliseconds(30));
        var buzzer = Accepted(Buzz(_opened, _zoe, pressedByMax.AddMilliseconds(20)));
        _time.Advance(TimeSpan.FromMilliseconds(100));
        buzzer = Accepted(Buzz(buzzer, _max, pressedByMax));
        var saved = JsonSerializer.Deserialize<Buzzer>(JsonSerializer.Serialize(buzzer, ContractJsonOptions.Default), ContractJsonOptions.Default)!;

        // When: resumed 3 minutes later
        var shift = TimeSpan.FromMinutes(3);
        var resumed = saved.Resume(shift);

        // Then: 150 ms left, and the same winner
        var arbitrateAt = _time.GetUtcNow() + shift + TimeSpan.FromMilliseconds(150);
        Assert.Equal(new ScheduleTimer(Buzzer.ArbitrationTimer, arbitrateAt), Assert.Single(resumed.Effects));
        Assert.Equal([_zoe, _max], resumed.Buzzer.Presses.Select(press => press.PlayerId));
        Assert.Equal(_max, resumed.Buzzer.Arbitrate(new TimerElapsed(Buzzer.ArbitrationTimer, arbitrateAt)).Buzzer.Winner);
    }

    [Fact]
    public void Resume_WinnerDesignated_SchedulesNothing()
    {
        // Given
        var won = Arbitrate(Accepted(Buzz(_opened, _zoe, PressedAgo(0)))).Buzzer;

        // When
        var resumed = won.Resume(TimeSpan.FromMinutes(3));

        // Then
        Assert.Empty(resumed.Effects);
        Assert.Equal(_zoe, resumed.Buzzer.Winner);
    }

    private static long Unix(DateTimeOffset time) => time.ToUnixTimeMilliseconds();

    private static Buzzer Accepted(BuzzerTransition transition)
    {
        Assert.Null(transition.Rejection);
        return transition.Buzzer;
    }

    private static void AssertRejected(Buzzer buzzer, BuzzerTransition transition, RejectionReason reason)
    {
        Assert.Equal(reason, transition.Rejection);
        Assert.Same(buzzer, transition.Buzzer);
        Assert.Empty(transition.Effects);
    }

    private GameContext Context() => new(_time.GetUtcNow(), new Random(42));

    /// <summary>A press <paramref name="milliseconds"/> before now, as the phone measured it in server time.</summary>
    private DateTimeOffset PressedAgo(int milliseconds) => _time.GetUtcNow().AddMilliseconds(-milliseconds);

    /// <summary>A buzz received now.</summary>
    private BuzzerTransition Buzz(Buzzer buzzer, PlayerId player, DateTimeOffset pressedAt, int opening = 1) =>
        buzzer.Buzz(player, opening, Unix(pressedAt), _time.GetUtcNow(), Context());

    /// <summary>The arbitration timer elapses on time.</summary>
    private BuzzerTransition Arbitrate(Buzzer buzzer)
    {
        _time.SetUtcNow(buzzer.ArbitrateAt!.Value);
        return buzzer.Arbitrate(new TimerElapsed(Buzzer.ArbitrationTimer, buzzer.ArbitrateAt.Value));
    }
}
