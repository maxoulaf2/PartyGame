namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// What a round of buzzer questions shows on the TV screen: the question once asked, then who has the hand. Never the
/// expected answer before the reveal, nor the time stamps of the buzzes.
/// </summary>
/// <param name="QuestionNumber">The question in progress, from 1.</param>
/// <param name="QuestionCount">How many questions the round has.</param>
/// <param name="Phase">Phase of the question in progress.</param>
/// <param name="Text">The text of the question, once asked, or <see langword="null"/> before.</param>
/// <param name="ImageUrl">The URL of the image of the question, once asked, or <see langword="null"/>.</param>
/// <param name="Winner">The nickname of the player who has the hand, or <see langword="null"/>.</param>
public sealed record BuzzerDisplayView(
    int QuestionNumber,
    int QuestionCount,
    BuzzerQuestionPhase Phase,
    string? Text,
    string? ImageUrl,
    string? Winner) : DisplayRoundView;
