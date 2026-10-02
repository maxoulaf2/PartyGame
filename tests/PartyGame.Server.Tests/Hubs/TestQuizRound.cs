using PartyGame.Engine.Modes;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// A round of <see cref="TestQuizMode"/>.
/// </summary>
/// <param name="PlayerIntents">How many intents the players sent in the round.</param>
internal sealed record TestQuizRound(int PlayerIntents) : RoundState;
