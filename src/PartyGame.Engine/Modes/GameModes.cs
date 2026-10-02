using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Modes;

/// <summary>
/// The game modes registered on this server, each found by the type of the descriptors it plays.
/// </summary>
public sealed class GameModes
{
    private readonly FrozenDictionary<Type, IGameMode> _byDescriptorType;

    /// <param name="modes">The registered modes, at most one per descriptor type.</param>
    /// <exception cref="ArgumentException">Two modes play the same descriptor type.</exception>
    public GameModes(IEnumerable<IGameMode> modes)
    {
        ArgumentNullException.ThrowIfNull(modes);

        var byDescriptorType = new Dictionary<Type, IGameMode>();
        foreach (var mode in modes)
        {
            if (!byDescriptorType.TryAdd(mode.DescriptorType, mode))
            {
                throw new ArgumentException(
                    $"Game modes {byDescriptorType[mode.DescriptorType].GetType().Name} and {mode.GetType().Name} both play {mode.DescriptorType.Name}.",
                    nameof(modes));
            }
        }

        _byDescriptorType = byDescriptorType.ToFrozenDictionary();
    }

    /// <summary>
    /// Finds the mode that plays an activity.
    /// </summary>
    /// <param name="descriptor">The activity of the pack.</param>
    /// <param name="mode">The mode registered for the type of <paramref name="descriptor"/>, if any.</param>
    public bool TryFind(RoundDescriptor descriptor, [NotNullWhen(true)] out IGameMode? mode)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return _byDescriptorType.TryGetValue(descriptor.GetType(), out mode);
    }

    /// <summary>
    /// The mode that plays an activity of a started game: the start checked that every activity has one.
    /// </summary>
    /// <param name="descriptor">The activity of the pack.</param>
    /// <exception cref="InvalidOperationException">No mode plays this activity: a bug.</exception>
    public IGameMode For(RoundDescriptor descriptor) =>
        TryFind(descriptor, out var mode)
            ? mode
            : throw new InvalidOperationException($"No game mode plays {descriptor.GetType().Name}.");
}
