namespace PartyGame.TypeGen;

/// <summary>
/// A C# construct the generator cannot translate faithfully. The message names the type and the property in cause.
/// </summary>
internal sealed class TypeGenException(string message) : Exception(message);
