namespace PartyGame.Engine.Inputs;

/// <summary>
/// What a client wants to do, enriched by the hub with its sender and the time it was received.
/// </summary>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public abstract record Intent(DateTimeOffset ReceivedAt) : GameInput;
