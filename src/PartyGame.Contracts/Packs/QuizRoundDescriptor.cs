using System.ComponentModel;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A multiple-choice quiz round.
/// </summary>
/// <remarks>
/// The first activity type. Its questions come with the quiz mode itself: until then, it only declares the type,
/// because System.Text.Json refuses a polymorphic base without any derived type.
/// </remarks>
[Description("Manche de quiz à choix multiples.")]
public sealed record QuizRoundDescriptor : RoundDescriptor;
