namespace PartyGame.Contracts;

/// <summary>
/// Messages the server sends to the clients through the SignalR hub: one method per message, named as its SignalR target.
/// The TypeScript client registers its handlers against the generated equivalent.
/// </summary>
/// <remarks>
/// Empty until the snapshots (US-E03-05): what a client receives so far only comes as the answer to its own calls.
/// </remarks>
public interface IGameClient;
