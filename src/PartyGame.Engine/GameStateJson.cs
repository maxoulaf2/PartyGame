using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Modes;

namespace PartyGame.Engine;

/// <summary>
/// The JSON options a <see cref="GameState"/> is persisted with: the conventions of the wire, plus the round states of the
/// registered game modes, which no attribute declares since <see cref="RoundState"/> does not know the modes.
/// </summary>
public static class GameStateJson
{
    /// <summary>
    /// Creates read-only options that serialize and deserialize a <see cref="GameState"/> and the round state of any of
    /// <paramref name="modes"/>, each identified by the name of its type.
    /// </summary>
    /// <param name="modes">The registered game modes.</param>
    /// <param name="baseOptions">The options to start from, <see cref="ContractJsonOptions.Default"/> unless given.</param>
    public static JsonSerializerOptions CreateOptions(GameModes modes, JsonSerializerOptions? baseOptions = null)
    {
        ArgumentNullException.ThrowIfNull(modes);

        var stateTypes = modes.StateTypes.ToArray();
        var options = new JsonSerializerOptions(baseOptions ?? ContractJsonOptions.Default);
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(typeInfo =>
        {
            if (typeInfo.Type != typeof(RoundState))
            {
                return;
            }

            // The name of the type rather than the type of the activity: renaming a round state changes the format of
            // the file, which its version tells.
            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions();
            foreach (var type in stateTypes)
            {
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(type, type.Name));
            }
        });
        options.MakeReadOnly();
        return options;
    }
}
