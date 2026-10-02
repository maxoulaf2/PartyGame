using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;
using PartyGame.Contracts;
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
    /// Checks the consistency of an activity of a pack with the mode that plays it, when the pack is loaded.
    /// </summary>
    /// <param name="descriptor">The activity of the pack.</param>
    /// <param name="path">The JSON path of the activity in the descriptor file, such as <c>$.rounds[1]</c>.</param>
    /// <returns>
    /// The problems found by the mode, or <see cref="PackProblemCode.PackRoundTypeUnknown"/> when no mode of this server
    /// plays the activity.
    /// </returns>
    public ImmutableArray<PackProblem> Validate(RoundDescriptor descriptor, string path)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (TryFind(descriptor, out var mode))
        {
            return mode.Validate(descriptor, path);
        }

        // The descriptor type is declared on RoundDescriptor, but its mode is not registered.
        var type = typeof(RoundDescriptor).GetCustomAttributes<JsonDerivedTypeAttribute>()
            .FirstOrDefault(derived => derived.DerivedType == descriptor.GetType())?.TypeDiscriminator?.ToString() ?? string.Empty;
        return [new PackProblem(
            PackProblemCode.PackRoundTypeUnknown,
            PackDescriptor.FileName,
            $"{path}.type",
            ImmutableDictionary<string, string>.Empty.Add("type", type))];
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
