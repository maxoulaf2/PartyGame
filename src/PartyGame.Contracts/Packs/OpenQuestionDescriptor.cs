using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A question of an open question round, with its expected answer and the variants accepted as well.
/// </summary>
[Description("Question d'une manche de questions ouvertes, avec sa réponse attendue et les variantes acceptées.")]
public sealed record OpenQuestionDescriptor
{
    /// <summary>
    /// The text of the question, shown on the TV screen as the countdown of the answers starts.
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    [Description("Texte de la question, affiché sur l'écran TV à l'ouverture des réponses.")]
    public required string Text { get; init; }

    /// <summary>
    /// An optional image, shown on the TV screen with the question.
    /// </summary>
    [Description("Image facultative, affichée sur l'écran TV avec la question : chemin d'un fichier .jpg, .jpeg, .png ou .webp du pack.")]
    public MediaPath? Image { get; init; }

    /// <summary>
    /// The time to answer this question, in seconds, or <see langword="null"/> to use the one of the round.
    /// </summary>
    [Range(OpenQuestionRoundDescriptor.MinAnswerSeconds, OpenQuestionRoundDescriptor.MaxAnswerSeconds)]
    [Description("Durée de réponse de cette question, en secondes, de 5 à 120. Remplace celle de la manche. Si absente, celle de la manche s'applique.")]
    public int? AnswerSeconds { get; init; }

    /// <summary>
    /// The expected answer, shown at the reveal. The open question mode checks it is no longer than the <c>maxLength</c> of
    /// the round.
    /// </summary>
    [StringLength(OpenQuestionRoundDescriptor.MaxMaxLength, MinimumLength = 1)]
    [Description("Réponse attendue, affichée à la révélation. Pas plus longue que la longueur maximale (maxLength) de la manche.")]
    public required string Answer { get; init; }

    /// <summary>
    /// Other answers accepted as correct. The open question mode checks each is 1 to <c>maxLength</c> characters long.
    /// Case, accents, punctuation, spaces and a leading article are ignored anyway: « Leonard de Vinci » needs no variant
    /// for « Léonard de Vinci ».
    /// </summary>
    [Length(0, 20)]
    [Description("Autres réponses acceptées, de 0 à 20, chacune de 1 caractère à la longueur maximale (maxLength) de la manche. La casse, les accents, la ponctuation, les espaces et un article initial sont déjà ignorés : inutile d'ajouter « leonard de vinci » pour « Léonard de Vinci ». Aucune si absent.")]
    public ImmutableArray<string> AcceptedAnswers { get; init; } = [];

    /// <summary>
    /// The keyboard the phones show to type the answer.
    /// </summary>
    [Description("Clavier affiché sur les téléphones : \"text\" pour un texte, \"numeric\" pour un nombre entier écrit en chiffres, comparé sans tolérance. \"text\" si absent.")]
    public OpenQuestionInputMode InputMode { get; init; } = OpenQuestionInputMode.Text;
}
