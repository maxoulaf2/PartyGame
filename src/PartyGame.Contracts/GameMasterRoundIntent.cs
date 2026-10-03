using System.Text.Json.Serialization;
using PartyGame.Contracts.Quiz;

namespace PartyGame.Contracts;

/// <summary>
/// What the game master wants to do in the round in progress (reveal, next question…), handed by the server to the game
/// mode of the round. Its <c>type</c> names the intent.
/// </summary>
/// <remarks>
/// Each game mode declares its own derived intents here with <see cref="JsonDerivedTypeAttribute"/>: these lines are
/// part of registering the mode. The <c>type</c> of an intent is the one of the rounds of its mode, a dot, then the
/// name of the intent, such as <c>quiz.openAnswers</c>: the clients tell from it which mode the intent belongs to. A
/// derived intent names the step it moves on from (question, phase), so that the mode rejects it as obsolete once the
/// game has moved on.
/// </remarks>
/// <param name="RoundId">
/// The round the intent is aimed at. The server rejects it without effect when that round is not the one in progress.
/// </param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(QuizOpenAnswers), "quiz.openAnswers")]
[JsonDerivedType(typeof(QuizLockAnswers), "quiz.lockAnswers")]
[JsonDerivedType(typeof(QuizRevealAnswer), "quiz.revealAnswer")]
[JsonDerivedType(typeof(QuizNextQuestion), "quiz.nextQuestion")]
[JsonDerivedType(typeof(QuizSkipQuestion), "quiz.skipQuestion")]
public abstract record GameMasterRoundIntent(RoundId RoundId);
