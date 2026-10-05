using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace PartyGame.Engine.Modes.OpenQuestion;

/// <summary>
/// How the answers to an open question are compared: once normalized, so that « L'Italie ! » and « italie » are the same.
/// </summary>
public static class OpenAnswers
{
    // Dropped when they open an answer. « L' » is « l » once its apostrophe is a space.
    private static readonly FrozenSet<string> _articles =
        FrozenSet.Create(StringComparer.Ordinal, "le", "la", "les", "l", "un", "une", "des", "the");

    /// <summary>
    /// The answer in lower case, without accents, with every punctuation mark or symbol turned into a space, spaces
    /// trimmed and collapsed, and without a leading article.
    /// </summary>
    /// <param name="answer">An answer, typed by a player or written in a pack.</param>
    /// <returns>The normalized answer, possibly empty (« ! », « Les »).</returns>
    public static string Normalize(string answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        var decomposed = answer.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
            }
        }

        var words = builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Length > 0 && _articles.Contains(words[0]) ? words[1..] : words);
    }
}
