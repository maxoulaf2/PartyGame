using System.Text.Json.Serialization;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Quiz;

namespace PartyGame.Contracts;

/// <summary>
/// What a player wants to do in the round in progress (answer, buzz…), handed by the server to the game mode of the
/// round. Its <c>type</c> names the intent.
/// </summary>
/// <remarks>
/// Each game mode declares its own derived intents here with <see cref="JsonDerivedTypeAttribute"/>: these lines are
/// part of registering the mode. The <c>type</c> of an intent is the one of the rounds of its mode, a dot, then the
/// name of the intent, such as <c>quiz.submitAnswer</c>: the clients tell from it which mode the intent belongs to.
/// </remarks>
/// <param name="RoundId">
/// The round the intent is aimed at. The server rejects it without effect when that round is not the one in progress.
/// </param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(QuizSubmitAnswer), "quiz.submitAnswer")]
[JsonDerivedType(typeof(BuzzerBuzz), "buzzer.buzz")]
[JsonDerivedType(typeof(BlindTestBuzz), "blindtest.buzz")]
public abstract record PlayerRoundIntent(RoundId RoundId);
