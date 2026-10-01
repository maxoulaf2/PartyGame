using PartyGame.Engine.Inputs;

namespace PartyGame.Server.Games;

/// <summary>
/// An input waiting in the queue, with the completion its producer awaits when it expects an answer.
/// </summary>
internal sealed record QueuedInput(GameInput Input, TaskCompletionSource<InputOutcome>? Completion);
