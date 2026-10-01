using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Server.Hubs;

/// <summary>
/// Reads the messages of the clients with the wire conventions, as SignalR would, but lets the hub log what it rejects.
/// </summary>
internal static class HubMessage
{
    /// <summary>
    /// Reads a message, or tells where it stops matching <typeparamref name="T"/>: a missing field, a wrong type, an unknown
    /// enum member.
    /// </summary>
    /// <param name="message">The message as received.</param>
    /// <param name="value">The message read, when it has the expected shape.</param>
    /// <param name="invalidPath">
    /// JSON path of the first problem, such as <c>$.role</c>. Never the value itself, which may be a secret like the game
    /// master code.
    /// </param>
    public static bool TryRead<T>(JsonElement message, [NotNullWhen(true)] out T? value, [NotNullWhen(false)] out string? invalidPath)
        where T : class
    {
        try
        {
            value = message.Deserialize<T>(ContractJsonOptions.Default);
        }
        catch (JsonException ex)
        {
            value = null;
            invalidPath = ex.Path ?? "$";
            return false;
        }

        invalidPath = value is null ? "$" : null;
        return value is not null;
    }
}
