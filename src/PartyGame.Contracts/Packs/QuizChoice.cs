using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A choice offered by a question of a quiz round. Exactly one choice of each question is correct.
/// </summary>
[Description("Proposition d'une question de quiz.")]
public sealed record QuizChoice
{
    /// <summary>
    /// The text of the choice, shown on the TV screen and on the phones.
    /// </summary>
    [StringLength(80, MinimumLength = 1)]
    [Description("Texte de la proposition, affiché sur l'écran TV et sur les téléphones.")]
    public required string Text { get; init; }

    /// <summary>
    /// Whether this choice is the correct answer of the question.
    /// </summary>
    [Description("Indique la bonne réponse : true sur une seule proposition de la question. Faux si absent.")]
    public bool Correct { get; init; }
}
