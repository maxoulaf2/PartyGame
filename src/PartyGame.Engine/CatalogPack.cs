using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine;

/// <summary>
/// A pack of the <see cref="PackCatalog"/>, loaded and checked by the server: a valid pack has a descriptor and no problem.
/// </summary>
/// <param name="Id">The identifier of the pack: the name of its folder.</param>
/// <param name="Title">The title of the pack, when the descriptor gives one, even if the pack is invalid.</param>
/// <param name="RoundCount">The number of activities of the pack, when the descriptor lists them, even if the pack is invalid.</param>
/// <param name="Descriptor">The descriptor of a valid pack, or <see langword="null"/> when the pack has problems.</param>
/// <param name="Problems">Every problem found in the pack: none when it is valid.</param>
public sealed record CatalogPack(
    string Id,
    string? Title,
    int? RoundCount,
    PackDescriptor? Descriptor,
    ImmutableArray<PackProblem> Problems)
{
    /// <summary>
    /// Whether the pack can be chosen: it has no problem.
    /// </summary>
    [JsonIgnore] // read from the descriptor, which is persisted
    [MemberNotNullWhen(true, nameof(Descriptor))]
    public bool IsValid => Descriptor is not null;

    /// <summary>
    /// The media files the descriptor of a valid pack references, each path once: the game draws an identifier for each
    /// when it starts. Empty when the pack is invalid.
    /// </summary>
    public ImmutableArray<MediaPath> Media { get; init; } = [];
}
