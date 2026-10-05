using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A question of a buzzer round, with the answer the game master expects.
/// </summary>
[Description("Question d'une manche de questions buzzer, avec la réponse attendue.")]
public sealed record BuzzerQuestion
{
    /// <summary>
    /// The text of the question, shown on the TV screen as the buzzer opens.
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    [Description("Texte de la question, affiché sur l'écran TV à l'ouverture du buzzer.")]
    public required string Text { get; init; }

    /// <summary>
    /// The expected answer: shown to the game master, who judges the answers given aloud, then to everyone at the reveal.
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    [Description("Réponse attendue, affichée au game master qui juge les réponses données à voix haute, puis à tous à la révélation.")]
    public required string Answer { get; init; }

    /// <summary>
    /// An optional image, shown on the TV screen with the question.
    /// </summary>
    [Description("Image facultative, affichée sur l'écran TV avec la question : chemin d'un fichier .jpg, .jpeg, .png ou .webp du pack.")]
    public MediaPath? Image { get; init; }
}
