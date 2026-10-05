namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// What a round of buzzer questions shows on the phone of one player: their buzzer, then who has the hand. The question
/// is read on the TV screen.
/// </summary>
/// <param name="QuestionNumber">The question in progress, from 1.</param>
/// <param name="QuestionCount">How many questions the round has.</param>
/// <param name="Opening">The current opening of the buzzer, which a buzz names.</param>
/// <param name="Buzzer">What the buzzer of this player shows.</param>
/// <param name="Winner">The nickname of the player who has the hand, or <see langword="null"/>.</param>
/// <param name="Points">
/// The points the player earned with the question, 0 included, once revealed; <see langword="null"/> before. Their total
/// is <see cref="PlayerSnapshot.Score"/>.
/// </param>
public sealed record BuzzerPlayerView(
    int QuestionNumber,
    int QuestionCount,
    int Opening,
    BuzzerButtonState Buzzer,
    string? Winner,
    int? Points) : PlayerRoundView;
