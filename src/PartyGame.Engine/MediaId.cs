using System.Text.Json.Serialization;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Engine;

/// <summary>
/// Identifies a media file of the pack the game plays, in its URL. Drawn at random when the game starts, it tells
/// nothing of the file: neither its name, nor its folder, nor its position in the pack.
/// </summary>
/// <param name="Value">The identifier, made of URL-safe characters only.</param>
[JsonConverter(typeof(TypedIdJsonConverterFactory))]
public readonly record struct MediaId(string Value);
