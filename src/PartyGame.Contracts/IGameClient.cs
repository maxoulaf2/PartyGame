namespace PartyGame.Contracts;

/// <summary>
/// Messages the server sends to the clients through the SignalR hub: one method per message, named as its SignalR target.
/// The TypeScript client registers its handlers against the generated equivalent.
/// </summary>
public interface IGameClient
{
    /// <summary>
    /// What the server tells every connection as soon as it is established, including a restored one.
    /// </summary>
    Task ReceiveWelcome(Welcome welcome);

    /// <summary>
    /// The current state of the game for the TV screen, sent after each change and right after the announcement.
    /// </summary>
    Task ReceiveDisplaySnapshot(DisplaySnapshot snapshot);

    /// <summary>
    /// The current state of the game for the game master, sent after each change and right after the announcement.
    /// </summary>
    Task ReceiveGameMasterSnapshot(GameMasterSnapshot snapshot);

    /// <summary>
    /// The current state of the game for one player, sent after each change and right after the identification.
    /// </summary>
    Task ReceivePlayerSnapshot(PlayerSnapshot snapshot);

    /// <summary>
    /// Every incident of the server, sent to the game master alone, on each new one and right after the announcement.
    /// </summary>
    Task ReceiveIncidents(IncidentList incidents);
}
