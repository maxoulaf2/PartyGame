using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// An activity of a pack, played as one round. Its <c>type</c> names the game mode that plays it.
/// </summary>
/// <remarks>
/// Each game mode declares its own derived descriptor here with <see cref="JsonDerivedTypeAttribute"/>: this line is
/// part of registering the mode.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(QuizRoundDescriptor), "quiz")]
[JsonDerivedType(typeof(BuzzerRoundDescriptor), "buzzer")]
[JsonDerivedType(typeof(BlindTestRoundDescriptor), "blindtest")]
[JsonDerivedType(typeof(OpenQuestionRoundDescriptor), "openquestion")]
[Description("Activité du pack, jouée comme une manche. Son type désigne le mode de jeu qui la joue.")]
public abstract record RoundDescriptor
{
    /// <summary>
    /// The title of the round, shown to everyone.
    /// </summary>
    [JsonPropertyOrder(-1)] // before the properties of the derived type, where an author of the pack looks for it
    [StringLength(60, MinimumLength = 1)]
    [Description("Titre de la manche, affiché à tous.")]
    public required string Title { get; init; }

    /// <summary>
    /// What the author of the pack tells of the round, shown with the rule of its mode when the round is announced, or
    /// <see langword="null"/> for the rule alone.
    /// </summary>
    [JsonPropertyOrder(-1)]
    [StringLength(300, MinimumLength = 1)]
    [Description("Présentation facultative de la manche, affichée avec la règle du mode quand la manche est annoncée.")]
    public string? Description { get; init; }
}
