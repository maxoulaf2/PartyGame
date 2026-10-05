using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A round of open questions: each player types an answer on their phone before the countdown ends, the server sorts the
/// answers out, and the game master validates them all at once.
/// </summary>
/// <remarks>
/// The rules that tie several values together (answers no longer than <see cref="MaxLength"/>, numeric answers, answers
/// that normalize to nothing or to the same) are checked by the open question mode when the pack is loaded.
/// </remarks>
[Description("Manche de questions ouvertes : chaque joueur tape sa réponse sur son téléphone avant la fin du compte à rebours, puis le game master valide les réponses d'un coup, aidé par un pré-classement du serveur.")]
public sealed record OpenQuestionRoundDescriptor : RoundDescriptor
{
    /// <summary>
    /// The time to answer a question when the round does not set one, in seconds.
    /// </summary>
    public const int DefaultAnswerSeconds = 30;

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
    /// The longest answer a player may type when the round does not set it, in characters.
    /// </summary>
    public const int DefaultMaxLength = 40;

    /// <summary>
    /// The lowest bound a round may set on the length of an answer, in characters.
    /// </summary>
    public const int MinMaxLength = 5;

    /// <summary>
    /// The highest bound a round may set on the length of an answer, in characters.
    /// </summary>
    public const int MaxMaxLength = 100;

    /// <summary>
    /// The time to answer each question of the round, in seconds, unless the question sets its own.
    /// </summary>
    [Range(MinAnswerSeconds, MaxAnswerSeconds)]
    [Description("Durée de réponse par défaut des questions, en secondes, de 5 à 120. 30 si absente.")]
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
    /// The longest answer a player may type, in characters: the expected answers and their variants are no longer.
    /// </summary>
    [Range(MinMaxLength, MaxMaxLength)]
    [Description("Longueur maximale d'une réponse tapée par un joueur, en caractères, de 5 à 100. 40 si absente. Les réponses attendues et leurs variantes ne doivent pas la dépasser.")]
    public int MaxLength { get; init; } = DefaultMaxLength;

    /// <summary>
    /// The questions of the round, asked in this order.
    /// </summary>
    [Length(1, 50)]
    [Description("Questions de la manche, de 1 à 50, posées dans cet ordre.")]
    public required ImmutableArray<OpenQuestionDescriptor> Questions { get; init; }
}
