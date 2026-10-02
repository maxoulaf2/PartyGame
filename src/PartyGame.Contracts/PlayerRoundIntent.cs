using System.Text.Json.Serialization;
using PartyGame.Contracts.Quiz;

namespace PartyGame.Contracts;

/// <summary>
/// What a player wants to do in the round in progress (answer, buzz…), handed by the server to the game mode of the
/// round. Its <c>type</c> names the intent.
/// </summary>
/// <remarks>
/// Each game mode declares its own derived intents here with <see cref="JsonDerivedTypeAttribute"/>: these lines are
/// part of registering the mode.
/// </remarks>
/// <param name="RoundId">
/// The round the intent is aimed at. The server rejects it without effect when that round is not the one in progress.
/// </param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(QuizPlayerIntent), "quiz")]
public abstract record PlayerRoundIntent(RoundId RoundId);
