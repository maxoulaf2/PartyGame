namespace PartyGame.Contracts;

/// <summary>
/// A phone asks to join the game under a nickname. The server generates the identity of the player: the phone sends
/// nothing else.
/// </summary>
/// <param name="Nickname">The nickname as typed. The server normalizes it and alone decides whether it is valid.</param>
public sealed record JoinRequest(string Nickname);
