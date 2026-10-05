namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// What a round of buzzer questions shows on the game master console: the whole question from the start, with its
/// expected answer, then who has the hand.
/// </summary>
/// <param name="QuestionNumber">The question in progress, from 1.</param>
/// <param name="QuestionCount">How many questions the round has.</param>
/// <param name="Phase">Phase of the question in progress.</param>
/// <param name="Opening">The current opening of the buzzer, which a judgment names.</param>
/// <param name="Text">The text of the question.</param>
/// <param name="Shown">Whether the TV screen shows the question.</param>
/// <param name="Answer">The expected answer, for the game master to judge the answers given out loud.</param>
/// <param name="Winner">The nickname of the player who has the hand, or <see langword="null"/>.</param>
/// <param name="FoundBy">
/// The nickname of the player whose answer was judged correct, once revealed, or <see langword="null"/>.
/// </param>
public sealed record BuzzerGameMasterView(
    int QuestionNumber,
    int QuestionCount,
    BuzzerQuestionPhase Phase,
    int Opening,
    string Text,
    bool Shown,
    string Answer,
    string? Winner,
    string? FoundBy) : GameMasterRoundView;
