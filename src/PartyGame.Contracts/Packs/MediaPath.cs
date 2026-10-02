using System.ComponentModel;
using System.Text.Json.Serialization;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// The path of a media file of a pack, as written in the descriptor: relative to the folder of the pack, with <c>/</c> as
/// separator. A plain string in <c>pack.json</c>.
/// </summary>
/// <remarks>
/// A dedicated type lets the loading find every media of a descriptor without knowing the game modes, to check that each
/// one exists. The clients never see it: they get an opaque identifier instead.
/// </remarks>
/// <param name="Value">The path as written in the descriptor.</param>
[JsonConverter(typeof(TypedIdJsonConverterFactory))]
[Description("Chemin d'un média du pack, relatif au dossier du pack, avec / pour séparateur (par exemple images/drapeau.png).")]
public readonly record struct MediaPath(string Value);
