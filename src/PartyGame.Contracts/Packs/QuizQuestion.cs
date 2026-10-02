using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A question of a quiz round, with its choices.
/// </summary>
[Description("Question d'une manche de quiz, avec ses propositions.")]
public sealed record QuizQuestion
{
    /// <summary>
    /// The text of the question, shown on the TV screen.
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    [Description("Texte de la question, affiché sur l'écran TV.")]
    public required string Text { get; init; }

    /// <summary>
    /// An optional image, shown on the TV screen only.
    /// </summary>
    [Description("Image facultative, affichée sur l'écran TV seulement : chemin d'un fichier .jpg, .jpeg, .png ou .webp du pack.")]
    public MediaPath? Image { get; init; }

    /// <summary>
    /// The time to answer this question, in seconds, or <see langword="null"/> to use the one of the round.
    /// </summary>
    [Range(QuizRoundDescriptor.MinAnswerSeconds, QuizRoundDescriptor.MaxAnswerSeconds)]
    [Description("Durée de réponse de cette question, en secondes, de 5 à 120. Remplace celle de la manche. Si absente, celle de la manche s'applique.")]
    public int? AnswerSeconds { get; init; }

    /// <summary>
    /// The choices, in the order of the descriptor unless the round shuffles them. Exactly one is correct.
    /// </summary>
    [Length(2, 4)]
    [Description("Propositions, de 2 à 4, dont une seule est la bonne réponse. Elles sont présentées dans cet ordre, sauf si la manche les mélange.")]
    public required ImmutableArray<QuizChoice> Choices { get; init; }
}
