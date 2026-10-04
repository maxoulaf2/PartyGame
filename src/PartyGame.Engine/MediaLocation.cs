using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine;

/// <summary>
/// Where the game shows a media file of its pack, for the game master to know what the TV screen could not show.
/// </summary>
/// <param name="Media">The path of the file in the pack, for the logs of the server only.</param>
/// <param name="Round">The round in progress, or <see langword="null"/> outside a round.</param>
/// <param name="Step">
/// The step of <paramref name="Round"/> that shows the file, from 1, such as the number of a quiz question, or
/// <see langword="null"/> when its game mode cannot tell.
/// </param>
public sealed record MediaLocation(MediaPath Media, RoundInfo? Round, int? Step);
