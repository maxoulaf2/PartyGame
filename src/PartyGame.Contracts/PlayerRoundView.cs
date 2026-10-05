using System.Text.Json.Serialization;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Quiz;

namespace PartyGame.Contracts;

/// <summary>
/// What the game mode of the round in progress shows on the phone of one player. Its <c>type</c> names the game mode.
/// </summary>
/// <remarks>
/// Each game mode declares its own derived view here with <see cref="JsonDerivedTypeAttribute"/>: this line is part of
/// registering the mode.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(QuizPlayerView), "quiz")]
[JsonDerivedType(typeof(BuzzerPlayerView), "buzzer")]
[JsonDerivedType(typeof(BlindTestPlayerView), "blindtest")]
[JsonDerivedType(typeof(OpenQuestionPlayerView), "openquestion")]
public abstract record PlayerRoundView;
