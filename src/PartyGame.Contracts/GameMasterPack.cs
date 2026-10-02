using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// A pack of the pack directory, as the game master sees it before choosing one: what it holds, or why it cannot be played.
/// </summary>
/// <param name="Id">The identifier of the pack, which a choice designates: the name of its folder.</param>
/// <param name="Title">The title of the pack, when its descriptor gives one, even if the pack is invalid.</param>
/// <param name="RoundCount">The number of rounds of the pack, when its descriptor lists them, even if the pack is invalid.</param>
/// <param name="IsValid">Whether the pack can be chosen: it has no problem.</param>
/// <param name="Rounds">The rounds of a valid pack, in the order they are played; none for an invalid pack.</param>
/// <param name="Problems">Every problem that makes the pack invalid; none for a valid pack.</param>
public sealed record GameMasterPack(
    string Id,
    string? Title,
    int? RoundCount,
    bool IsValid,
    ImmutableArray<GameMasterPackRound> Rounds,
    ImmutableArray<PackProblem> Problems);
