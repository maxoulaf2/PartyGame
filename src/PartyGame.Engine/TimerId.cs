using System.Text.Json.Serialization;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Engine;

/// <summary>
/// Identifies a timer requested by the engine, so that it can be replaced, cancelled, or recognized when it elapses.
/// </summary>
/// <param name="Value">A name chosen by the engine, such as <c>question-countdown</c>.</param>
[JsonConverter(typeof(TypedIdJsonConverterFactory))]
public readonly record struct TimerId(string Value);
