using System.Text.Json.Serialization;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Quiz;

namespace PartyGame.Contracts;

/// <summary>
/// What the game mode of the round in progress shows on the TV screen. Public by construction, like
/// <see cref="DisplaySnapshot"/>. Its <c>type</c> names the game mode.
/// </summary>
/// <remarks>
/// Each game mode declares its own derived view here with <see cref="JsonDerivedTypeAttribute"/>: this line is part of
/// registering the mode.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(QuizDisplayView), "quiz")]
[JsonDerivedType(typeof(BuzzerDisplayView), "buzzer")]
[JsonDerivedType(typeof(BlindTestDisplayView), "blindtest")]
[JsonDerivedType(typeof(OpenQuestionDisplayView), "openquestion")]
public abstract record DisplayRoundView;
