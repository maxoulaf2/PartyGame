using PartyGame.Engine;

namespace PartyGame.Server.Games;

/// <summary>
/// What became of a submitted input, answered by the loop once it has handled it.
/// </summary>
/// <param name="Status">Whether the input was accepted, rejected, or failed.</param>
/// <param name="Rejection">Why the engine rejected the input, when <paramref name="Status"/> is <see cref="InputStatus.Rejected"/>.</param>
internal sealed record InputOutcome(InputStatus Status, RejectionReason? Rejection = null)
{
    public static readonly InputOutcome Accepted = new(InputStatus.Accepted);

    public static readonly InputOutcome Failed = new(InputStatus.Failed);

    public static InputOutcome Rejected(RejectionReason reason) => new(InputStatus.Rejected, reason);
}
