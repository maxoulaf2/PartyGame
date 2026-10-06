using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master corrects the score of a player. Only a connection authenticated as game master gets it past the hub.
/// </summary>
/// <param name="PlayerId">The player whose score changes.</param>
/// <param name="ExpectedScore">The score corrected: the request is obsolete once the score is another.</param>
/// <param name="NewScore">The new total of the player.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record AdjustScore(PlayerId PlayerId, int ExpectedScore, int NewScore, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
