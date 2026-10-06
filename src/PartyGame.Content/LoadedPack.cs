using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// A pack found in the pack directory, loaded and checked once and for all: the descriptor is never read again during a
/// game.
/// </summary>
/// <param name="Id">The identifier of the pack: the name of its folder, or of its zip without the extension.</param>
/// <param name="Folder">The full path of the folder of the pack, or of its zip file.</param>
/// <param name="Title">The title of the pack, when the descriptor gives one, even if the pack is invalid.</param>
/// <param name="RoundCount">The number of activities of the pack, when the descriptor lists them, even if the pack is invalid.</param>
/// <param name="Descriptor">The descriptor of a valid pack, or <see langword="null"/> when the pack has problems.</param>
/// <param name="Problems">Every problem found in the pack: none when it is valid.</param>
public sealed record LoadedPack(
    string Id,
    string Folder,
    string? Title,
    int? RoundCount,
    PackDescriptor? Descriptor,
    ImmutableArray<PackProblem> Problems)
{
    /// <summary>
    /// Whether the pack can be played: it has no problem.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Descriptor))]
    public bool IsValid => Descriptor is not null;

    /// <summary>
    /// The unexpected error that interrupted the loading, reported as <see cref="PackProblemCode.PackLoadFailed"/>: a bug
    /// to log, never shown to the game master.
    /// </summary>
    public Exception? Failure { get; init; }

    /// <summary>
    /// The media files the descriptor of a valid pack references, each path once, in the order of the descriptor. Empty
    /// when the pack is invalid.
    /// </summary>
    public ImmutableArray<MediaPath> Media { get; init; } = [];
}
