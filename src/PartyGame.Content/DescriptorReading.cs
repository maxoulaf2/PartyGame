using System.Collections.Immutable;
using PartyGame.Contracts;

namespace PartyGame.Content;

/// <summary>
/// What <see cref="DescriptorReader"/> found in a descriptor.
/// </summary>
/// <param name="Problems">The structure and constraint problems, in the order of the file.</param>
/// <param name="Media">Every media file referenced by the parts that could be read.</param>
/// <param name="Excerpts">Every audio excerpt of the parts that could be read.</param>
/// <param name="Rounds">The activities that could be read, even when other parts of the descriptor could not.</param>
internal sealed record DescriptorReading(
    ImmutableArray<PackProblem> Problems,
    ImmutableArray<MediaReference> Media,
    ImmutableArray<ExcerptReference> Excerpts,
    ImmutableArray<RoundReference> Rounds);
