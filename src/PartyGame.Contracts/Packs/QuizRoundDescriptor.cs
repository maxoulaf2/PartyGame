using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A multiple-choice quiz round: the game master presents each question, opens the answers, then reveals the correct one.
/// </summary>
/// <remarks>
/// The rules that tie several values together (exactly one correct choice, no duplicated choice) are checked by the quiz
/// mode when the pack is loaded.
/// </remarks>
[Description("Manche de quiz à choix multiples : le game master présente chaque question, ouvre les réponses, puis révèle la bonne.")]
public sealed record QuizRoundDescriptor : RoundDescriptor
{
    /// <summary>
    /// The time to answer a question when the round does not set one, in seconds.
    /// </summary>
    public const int DefaultAnswerSeconds = 20;

    /// <summary>
    /// The shortest time to answer a question, in seconds.
    /// </summary>
    public const int MinAnswerSeconds = 5;

    /// <summary>
    /// The longest time to answer a question, in seconds.
    /// </summary>
    public const int MaxAnswerSeconds = 120;

    /// <summary>
    /// The points of a correct answer when the round does not set them.
    /// </summary>
    public const int DefaultPoints = 1000;

    /// <summary>
    /// The highest points of a correct answer, and the highest speed bonus.
    /// </summary>
    public const int MaxPoints = 10_000;

    /// <summary>
    /// The time to answer each question of the round, in seconds, unless the question sets its own.
    /// </summary>
    [Range(MinAnswerSeconds, MaxAnswerSeconds)]
    [Description("Durée de réponse par défaut des questions, en secondes, de 5 à 120. 20 si absente.")]
    public int AnswerSeconds { get; init; } = DefaultAnswerSeconds;

    /// <summary>
    /// The points of a correct answer.
    /// </summary>
    [Range(0, MaxPoints)]
    [Description("Points d'une bonne réponse, de 0 à 10 000. 1 000 si absent.")]
    public int Points { get; init; } = DefaultPoints;

    /// <summary>
    /// The highest speed bonus, added to the points of a correct answer in proportion to the time left. Zero means no bonus.
    /// </summary>
    [Range(0, MaxPoints)]
    [Description("Bonus de rapidité maximal, de 0 à 10 000, ajouté aux points d'une bonne réponse en proportion du temps restant. 0 si absent, c'est-à-dire sans bonus.")]
    public int SpeedBonus { get; init; }

    /// <summary>
    /// Whether the choices of each question are shuffled, rather than presented in the order of the descriptor.
    /// </summary>
    [Description("Mélange les propositions de chaque question au lieu de les présenter dans l'ordre du descripteur. Faux si absent.")]
    public bool ShuffleChoices { get; init; }

    /// <summary>
    /// The questions of the round, played in this order.
    /// </summary>
    [Length(1, 50)]
    [Description("Questions de la manche, de 1 à 50, posées dans cet ordre.")]
    public required ImmutableArray<QuizQuestion> Questions { get; init; }
}
