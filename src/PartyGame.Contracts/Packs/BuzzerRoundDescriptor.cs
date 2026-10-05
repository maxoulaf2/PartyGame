using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A round of buzzer questions: the game master asks each question, the first player to buzz answers aloud, and the game
/// master judges the answer.
/// </summary>
[Description("Manche de questions buzzer : le game master pose chaque question, le premier joueur qui buzze répond à voix haute, et le game master juge sa réponse.")]
public sealed record BuzzerRoundDescriptor : RoundDescriptor
{
    /// <summary>
    /// The points of a correct answer when the round does not set them.
    /// </summary>
    public const int DefaultPoints = 1000;

    /// <summary>
    /// The highest points of a correct answer.
    /// </summary>
    public const int MaxPoints = 10_000;

    /// <summary>
    /// The points of a correct answer.
    /// </summary>
    [Range(0, MaxPoints)]
    [Description("Points d'une bonne réponse, de 0 à 10 000. 1 000 si absent.")]
    public int Points { get; init; } = DefaultPoints;

    /// <summary>
    /// The questions of the round, asked in this order.
    /// </summary>
    [Length(1, 50)]
    [Description("Questions de la manche, de 1 à 50, posées dans cet ordre.")]
    public required ImmutableArray<BuzzerQuestion> Questions { get; init; }
}
