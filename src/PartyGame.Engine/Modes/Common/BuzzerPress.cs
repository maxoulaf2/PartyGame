using PartyGame.Contracts;

namespace PartyGame.Engine.Modes.Common;

/// <summary>
/// A buzz retained during the arbitration window of an opening of the buzzer.
/// </summary>
/// <param name="PlayerId">The player who buzzed.</param>
/// <param name="PressedAt">
/// When the player pressed, in server time, as their phone measured it and kept within bounds: never after the buzz was
/// received, never before the buzzer opened, nor more than <see cref="Buzzer.MaxPressAge"/> before the buzz was received.
/// </param>
/// <param name="ReceivedAt">Server time at which the hub received the buzz.</param>
public sealed record BuzzerPress(PlayerId PlayerId, DateTimeOffset PressedAt, DateTimeOffset ReceivedAt);
