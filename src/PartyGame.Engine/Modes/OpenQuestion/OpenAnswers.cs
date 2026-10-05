using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Packs;

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

    /// <summary>
    /// How an answer compares to the expected answer of a question and its variants: equal to one of them, close to one,
    /// within a tolerance for typos that grows with the length of the expected answer, or neither. A numeric answer has no
    /// tolerance: 1968 is not 1969.
    /// </summary>
    /// <param name="normalized">An answer, already normalized.</param>
    /// <param name="question">The question answered.</param>
    public static OpenQuestionAnswerCategory Classify(string normalized, OpenQuestionDescriptor question)
    {
        ArgumentNullException.ThrowIfNull(normalized);
        ArgumentNullException.ThrowIfNull(question);

        var distance = question.AcceptedAnswers.Prepend(question.Answer).Min(answer => Distance(normalized, Normalize(answer)));
        var expectedLength = Normalize(question.Answer).Length;
        var tolerance = question.InputMode == OpenQuestionInputMode.Numeric || expectedLength <= 3 ? 0 : expectedLength <= 7 ? 1 : 2;
        return distance == 0 ? OpenQuestionAnswerCategory.Accepted
            : distance <= tolerance ? OpenQuestionAnswerCategory.ToCheck
            : OpenQuestionAnswerCategory.Rejected;
    }

    /// <summary>
    /// The Levenshtein distance between two texts: how many characters to insert, delete or replace to turn one into the
    /// other.
    /// </summary>
    public static int Distance(string a, string b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        // Two rows of the matrix are enough: each one only reads the previous one.
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var replace = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(replace, Math.Min(previous[j], current[j - 1]) + 1);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
