using System.Collections.Immutable;
using System.Text.Json.Serialization;
using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes.Common;

/// <summary>
/// The buzzer of a round, which a game mode keeps in the state of its round and calls from its <c>Handle</c>. The winner
/// is the player who pressed first, from the time stamp of their phone, not the first buzz to arrive: once the first buzz
/// arrives, the buzzer waits for an arbitration window before it decides. Like the rest of the engine, it is pure.
/// </summary>
/// <remarks>
/// A fresh buzzer is closed. Each opening is numbered, and a buzz names the opening it is aimed at, so that a buzz sent
/// again after a reconnection never wins a later one.
/// </remarks>
public sealed record Buzzer
{
    /// <summary>
    /// The timer of the arbitration window, scheduled in the name of the round when the first buzz of an opening arrives.
    /// </summary>
    public static readonly TimerId ArbitrationTimer = new("buzzer-arbitration");

    /// <summary>
    /// How long before its reception a buzz may have been pressed: an earlier time stamp comes from a clock out of sync or
    /// a forged message, and is brought back to this bound.
    /// </summary>
    public static readonly TimeSpan MaxPressAge = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The number of the current or last opening, from 1, or 0 before the first one.
    /// </summary>
    public int Opening { get; init; }

    /// <summary>
    /// When the current or last opening started.
    /// </summary>
    public DateTimeOffset OpenedAt { get; init; }

    /// <summary>
    /// When the arbitration window of the current opening ends, set when its first buzz arrives, or <see langword="null"/>
    /// before.
    /// </summary>
    public DateTimeOffset? ArbitrateAt { get; init; }

    /// <summary>
    /// The buzzes of the current opening, in order of reception: one per player at most.
    /// </summary>
    public ImmutableArray<BuzzerPress> Presses { get; init; } = [];

    /// <summary>
    /// The players who may not buzz anymore until the buzzer opens anew with <see cref="Open"/>.
    /// </summary>
    public ImmutableArray<PlayerId> Blocked { get; init; } = [];

    /// <summary>
    /// The winner of the current opening, or <see langword="null"/> while it is open, before the first one, and once closed.
    /// </summary>
    public PlayerId? Winner { get; init; }

    /// <summary>
    /// Whether the buzzer was closed with <see cref="Close"/> since its last opening.
    /// </summary>
    public bool IsClosed { get; init; }

    /// <summary>
    /// Whether the buzzer accepts buzzes: opened, not closed, and no winner designated yet.
    /// </summary>
    [JsonIgnore] // derived from the opening, the closing and the winner, which are persisted
    public bool IsOpen => Opening > 0 && !IsClosed && Winner is null;

    /// <summary>
    /// Opens the buzzer to every player, for instance for a new question: nobody stays blocked.
    /// </summary>
    /// <param name="now">Current server time.</param>
    public Buzzer Open(DateTimeOffset now) => Reopen(now) with { Blocked = [] };

    /// <summary>
    /// Opens the buzzer anew to the players who are not blocked, for instance after a wrong answer.
    /// </summary>
    /// <param name="now">Current server time.</param>
    public Buzzer Reopen(DateTimeOffset now) =>
        this with { Opening = Opening + 1, OpenedAt = now, ArbitrateAt = null, Presses = [], Winner = null, IsClosed = false };

    /// <summary>
    /// Closes the buzzer until it opens anew, for instance once the answer is revealed: an arbitration window in progress
    /// is abandoned, and nobody has the hand. The players stay blocked.
    /// </summary>
    public Buzzer Close() => this with { IsClosed = true, ArbitrateAt = null, Presses = [], Winner = null };

    /// <summary>
    /// Blocks a player until the buzzer opens anew with <see cref="Open"/>, for instance after their wrong answer.
    /// </summary>
    /// <param name="player">The player to block.</param>
    public Buzzer Block(PlayerId player) => Blocked.Contains(player) ? this : this with { Blocked = Blocked.Add(player) };

    /// <summary>
    /// Retains a buzz. The first one of the opening starts the arbitration window, and buzzes received during it are
    /// retained too.
    /// </summary>
    /// <param name="player">The player who buzzed.</param>
    /// <param name="opening">The opening the buzz is aimed at.</param>
    /// <param name="pressedAtUnixMilliseconds">
    /// When the player pressed, as their phone measured it in server time, in milliseconds since the Unix epoch. Any value
    /// is accepted, brought back within bounds.
    /// </param>
    /// <param name="receivedAt">Server time at which the hub received the buzz.</param>
    /// <param name="context">Current time, and the duration of the arbitration window.</param>
    public BuzzerTransition Buzz(PlayerId player, int opening, long pressedAtUnixMilliseconds, DateTimeOffset receivedAt, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        RejectionReason? rejection =
            opening != Opening ? RejectionReason.BuzzerOpeningMismatch
            : !IsOpen || receivedAt > ArbitrateAt ? RejectionReason.BuzzerClosed
            : Blocked.Contains(player) ? RejectionReason.PlayerBlocked
            : Presses.Any(press => press.PlayerId == player) ? RejectionReason.AlreadyBuzzed
            : null;
        if (rejection is { } reason)
        {
            return BuzzerTransition.Rejected(this, reason);
        }

        // In milliseconds, so that no forged value overflows a date: a clock ahead counts for the arrival of the buzz, and
        // a press can come neither before the opening nor too long before its arrival.
        var received = receivedAt.ToUnixTimeMilliseconds();
        var earliest = Math.Max(OpenedAt.ToUnixTimeMilliseconds(), received - (long)MaxPressAge.TotalMilliseconds);
        var pressedAt = DateTimeOffset.FromUnixTimeMilliseconds(Math.Max(Math.Min(pressedAtUnixMilliseconds, received), earliest));

        var buzzed = this with { Presses = Presses.Add(new BuzzerPress(player, pressedAt, receivedAt)) };
        if (ArbitrateAt is not null)
        {
            return new(buzzed, []);
        }

        var arbitrateAt = context.Now + context.BuzzerArbitrationWindow;
        return new(buzzed with { ArbitrateAt = arbitrateAt }, [new ScheduleTimer(ArbitrationTimer, arbitrateAt)]);
    }

    /// <summary>
    /// Designates the winner at the end of the arbitration window: the earliest press, and the first received among equal
    /// ones. The timer of another opening, or of an opening already decided, is obsolete.
    /// </summary>
    /// <param name="timer">The timer that elapsed.</param>
    public BuzzerTransition Arbitrate(TimerElapsed timer)
    {
        ArgumentNullException.ThrowIfNull(timer);

        if (timer.TimerId != ArbitrationTimer || !IsOpen || timer.DueAt != ArbitrateAt)
        {
            return BuzzerTransition.Rejected(this, RejectionReason.UnexpectedTimer);
        }

        // A stable sort: the order of reception settles a tie.
        var winner = Presses.OrderBy(press => press.PressedAt).ThenBy(press => press.ReceivedAt).First();
        return new(this with { Winner = winner.PlayerId }, []);
    }

    /// <summary>
    /// Resumes the buzzer of a game saved before the server stopped: its times move on by the time spent offline, and an
    /// arbitration window in progress goes on with the time it had left.
    /// </summary>
    /// <param name="shift">The time spent offline: now minus the time of the last save.</param>
    public BuzzerTransition Resume(TimeSpan shift)
    {
        var resumed = this with
        {
            OpenedAt = OpenedAt + shift,
            ArbitrateAt = ArbitrateAt + shift,
            Presses = [.. Presses.Select(press => press with { PressedAt = press.PressedAt + shift, ReceivedAt = press.ReceivedAt + shift })],
        };
        return IsOpen && resumed.ArbitrateAt is { } arbitrateAt
            ? new(resumed, [new ScheduleTimer(ArbitrationTimer, arbitrateAt)])
            : new(resumed, []);
    }
}
