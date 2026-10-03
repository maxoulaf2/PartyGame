using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// A round of <see cref="TestQuizMode"/>.
/// </summary>
/// <param name="Quiz">The round as the quiz mode plays it, for its views.</param>
/// <param name="PlayerIntents">How many intents the players sent in the round.</param>
internal sealed record TestQuizRound(QuizRound Quiz, int PlayerIntents) : RoundState;
