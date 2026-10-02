using System.Globalization;
using System.Text;

namespace PartyGame.Engine.Text;

/// <summary>
/// How players tell two texts apart: not by case, accents or spaces. Shared by the nicknames (« Zoé » and « zoe ») and the
/// choices of a quiz question (« Le Mans » and « le  mans »).
/// </summary>
internal static class TextComparison
{
    /// <summary>
    /// A key equal for two texts players take for the same: white space trimmed and collapsed, accents removed, upper case.
    /// </summary>
    /// <param name="text">Any text.</param>
    public static string Key(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSpace = false;
        foreach (var character in decomposed)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString().ToUpperInvariant();
    }
}
