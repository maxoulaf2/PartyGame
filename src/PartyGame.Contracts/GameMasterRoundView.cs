using System.Text.Json.Serialization;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Quiz;

namespace PartyGame.Contracts;

/// <summary>
/// What the game mode of the round in progress shows on the game master console, the only view allowed to hold the
/// answers before their reveal. Its <c>type</c> names the game mode.
/// </summary>
/// <remarks>
/// Each game mode declares its own derived view here with <see cref="JsonDerivedTypeAttribute"/>: this line is part of
/// registering the mode.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(QuizGameMasterView), "quiz")]
[JsonDerivedType(typeof(BuzzerGameMasterView), "buzzer")]
[JsonDerivedType(typeof(BlindTestGameMasterView), "blindtest")]
public abstract record GameMasterRoundView;
