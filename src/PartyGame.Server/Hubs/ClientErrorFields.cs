using System.Diagnostics.CodeAnalysis;

namespace PartyGame.Server.Hubs;

/// <summary>
/// The longest each field of a <see cref="Contracts.ClientErrorReport"/> may be once logged. The pages cut them to the
/// same lengths; the server cuts them again, since a page may be modified.
/// </summary>
internal static class ClientErrorFields
{
    public const int MaxPageLength = 200;
    public const int MaxMessageLength = 500;
    public const int MaxStackLength = 2000;
    public const int MaxRoundViewTypeLength = 64;
    public const int MaxBuildIdLength = 64;

    /// <summary>
    /// <paramref name="value"/>, cut to <paramref name="maxLength"/> characters at most, never in the middle of a
    /// surrogate pair.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Truncate(string? value, int maxLength)
    {
        if (value is null || value.Length <= maxLength)
        {
            return value;
        }

        var length = char.IsHighSurrogate(value[maxLength - 1]) ? maxLength - 1 : maxLength;
        return value[..length];
    }
}
