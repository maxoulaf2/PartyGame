namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What a player wants to do in a quiz round.
/// </summary>
/// <remarks>
/// A placeholder until the quiz mode is written (E08), which replaces it with its own intents: System.Text.Json refuses
/// a polymorphic base without any derived type. The server rejects it, since no game mode plays quiz rounds yet.
/// </remarks>
/// <param name="RoundId">The round the intent is aimed at.</param>
public sealed record QuizPlayerIntent(RoundId RoundId) : PlayerRoundIntent(RoundId);
