namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What a quiz round shows on the phone of one player.
/// </summary>
/// <remarks>
/// Empty until the quiz mode is written (E08): System.Text.Json refuses a polymorphic base without any derived type, so
/// the first game mode declares its view as soon as the base exists.
/// </remarks>
public sealed record QuizPlayerView : PlayerRoundView;
