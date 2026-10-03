namespace PartyGame.Contracts;

/// <summary>
/// An intent of a player, numbered by their phone so that sending it again never has it handled twice: after a lost
/// connection, the phone cannot tell whether the server received what it sent, and sends it again with the same number.
/// </summary>
/// <param name="ClientSeq">
/// The number of the intent, from 1, increasing with each intent of the player and kept by the phone with their token.
/// The server ignores an intent whose number is not greater than the last one it handled for this player.
/// </param>
/// <param name="Intent">What the player wants to do in the round in progress.</param>
public sealed record PlayerIntentEnvelope(long ClientSeq, PlayerRoundIntent Intent);
