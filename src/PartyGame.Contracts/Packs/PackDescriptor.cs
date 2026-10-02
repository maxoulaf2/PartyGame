using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// The <c>pack.json</c> file at the root of a pack folder, read with <see cref="PackJsonOptions"/>. It is written by
/// hand by pack authors and never sent to the clients.
/// </summary>
/// <remarks>
/// The data annotations are simple constraints, translated into <c>schemas/pack.schema.json</c> for the editor and
/// checked again when the pack is loaded. The <see cref="DescriptionAttribute"/> texts document the format for the
/// authors in the schema: they are in French, like the rest of the documentation.
/// </remarks>
[Description("Descripteur d'un pack de contenu PartyGame : le fichier pack.json à la racine du dossier du pack.")]
public sealed record PackDescriptor
{
    /// <summary>
    /// The only version of the format the server reads.
    /// </summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>
    /// The location of the JSON Schema, for the editor only: the server ignores it.
    /// </summary>
    [JsonPropertyName("$schema")]
    [Description("Chemin du schéma JSON, pour l'autocomplétion dans l'éditeur. Ignoré par le serveur.")]
    public string? Schema { get; init; }

    /// <summary>
    /// The version of the format the descriptor is written in.
    /// </summary>
    [Range(CurrentFormatVersion, CurrentFormatVersion)]
    [Description("Version du format du descripteur. Seule la version 1 est prise en charge.")]
    public required int FormatVersion { get; init; }

    /// <summary>
    /// The title of the pack, shown to the game master when choosing a pack and to everyone during the game.
    /// </summary>
    [StringLength(60, MinimumLength = 1)]
    [Description("Titre du pack, affiché au game master lors du choix du pack, puis à tous pendant la partie.")]
    public required string Title { get; init; }

    /// <summary>
    /// An optional summary of the pack, for the game master.
    /// </summary>
    [StringLength(200)]
    [Description("Présentation facultative du pack, destinée au game master.")]
    public string? Description { get; init; }

    /// <summary>
    /// The activities of the pack, played in this order. Each one is a round of the game.
    /// </summary>
    [MinLength(1)]
    [Description("Activités du pack, jouées dans cet ordre : chacune est une manche de la partie.")]
    public required ImmutableArray<RoundDescriptor> Rounds { get; init; }
}
