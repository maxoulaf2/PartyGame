using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Rules of nicknames, shared by the registration and the renaming by the game master.
/// </summary>
public static class NicknameRules
{
    /// <summary>
    /// Maximum length of a nickname, in visible characters (grapheme clusters): an emoji counts as one.
    /// </summary>
    public const int MaxLength = 16;

    /// <summary>
    /// Characters that render as blank although Unicode classifies them as letters or symbols,
    /// a known way to fake an empty nickname.
    /// </summary>
    private static readonly SearchValues<char> _blankCharacters =
        SearchValues.Create("͏ᅟᅠ⠀ㅤﾠ");

    /// <summary>
    /// Normalizes a nickname as typed: spaces trimmed and collapsed, composed Unicode form.
    /// </summary>
    /// <param name="text">The nickname as typed.</param>
    /// <param name="nickname">The normalized nickname, when it is valid.</param>
    /// <returns>
    /// <see langword="false"/> when the nickname is empty, longer than <see cref="MaxLength"/>,
    /// or contains control or invisible characters.
    /// </returns>
    public static bool TryNormalize(string text, [NotNullWhen(true)] out string? nickname)
    {
        ArgumentNullException.ThrowIfNull(text);
        nickname = null;

        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;
        var index = 0;
        while (index < text.Length)
        {
            var remaining = text.AsSpan(index);
            if (Rune.DecodeFromUtf16(remaining, out var rune, out var length) != OperationStatus.Done)
            {
                return false; // a lone surrogate
            }

            index += length;
            var category = Rune.GetUnicodeCategory(rune);
            if (category == UnicodeCategory.SpaceSeparator)
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (category is UnicodeCategory.Control or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator
                || remaining[..length].ContainsAny(_blankCharacters))
            {
                return false;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(remaining[..length]);
        }

        var normalized = builder.ToString().Normalize(NormalizationForm.FormC);
        var graphemes = 0;
        var elements = StringInfo.GetTextElementEnumerator(normalized);
        while (elements.MoveNext())
        {
            if (!IsVisible(elements.GetTextElement()) || ++graphemes > MaxLength)
            {
                return false;
            }
        }

        if (graphemes == 0)
        {
            return false;
        }

        nickname = normalized;
        return true;
    }

    /// <summary>
    /// Whether two normalized nicknames are the same for players, ignoring case and accents (« Zoé » and « zoe »).
    /// </summary>
    /// <param name="first">A normalized nickname.</param>
    /// <param name="second">Another normalized nickname.</param>
    public static bool AreSame(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return string.Equals(ComparisonKey(first), ComparisonKey(second), StringComparison.Ordinal);
    }

    /// <summary>
    /// A grapheme is visible when it starts with a visible character. Format characters are only allowed inside it,
    /// and only those that build emoji sequences: the zero width joiner (families) and tags (subdivision flags).
    /// </summary>
    private static bool IsVisible(string grapheme)
    {
        var first = true;
        foreach (var rune in grapheme.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (first && category is UnicodeCategory.Format or UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark)
            {
                return false;
            }

            if (!first && category == UnicodeCategory.Format && rune.Value != 0x200D && rune.Value is < 0xE0020 or > 0xE007F)
            {
                return false;
            }

            first = false;
        }

        return true;
    }

    private static string ComparisonKey(string nickname)
    {
        var decomposed = nickname.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().ToUpperInvariant();
    }
}
