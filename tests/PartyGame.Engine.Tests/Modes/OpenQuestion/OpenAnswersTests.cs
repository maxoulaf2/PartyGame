using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes.OpenQuestion;

namespace PartyGame.Engine.Tests.Modes.OpenQuestion;

public sealed class OpenAnswersTests
{
    [Theory]
    [InlineData("Paris", "paris")]
    [InlineData("  Léonard   de Vinci ", "leonard de vinci")]
    [InlineData("L'Italie", "italie")]
    [InlineData("L’Italie !", "italie")]
    [InlineData("Les Beatles", "beatles")]
    [InlineData("The Beatles", "beatles")]
    [InlineData("Une souris verte", "souris verte")]
    [InlineData("Saint-Étienne", "saint etienne")]
    [InlineData("AC/DC", "ac dc")]
    [InlineData("« Le Mans ».", "mans")]
    [InlineData("1789", "1789")]
    public void Normalize_Answer_IgnoresCaseAccentsPunctuationSpacesAndLeadingArticle(string answer, string expected) =>
        Assert.Equal(expected, OpenAnswers.Normalize(answer));

    [Theory]
    [InlineData("Paris les Bains", "paris les bains")]
    [InlineData("Lesotho", "lesotho")]
    [InlineData("Des des", "des")]
    public void Normalize_ArticleNotLeadingOrPartOfAWord_IsKept(string answer, string expected) =>
        Assert.Equal(expected, OpenAnswers.Normalize(answer));

    [Theory]
    [InlineData("")]
    [InlineData("!")]
    [InlineData("Les")]
    [InlineData(" l' ")]
    public void Normalize_NothingButPunctuationOrAnArticle_IsEmpty(string answer) =>
        Assert.Equal("", OpenAnswers.Normalize(answer));

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("vinci", "", 5)]
    [InlineData("vinci", "vinci", 0)]
    [InlineData("vinchi", "vinci", 1)]
    [InlineData("vnci", "vinci", 1)]
    [InlineData("vinic", "vinci", 2)]
    [InlineData("kitten", "sitting", 3)]
    public void Distance_TwoTexts_CountsTheEditsBetweenThem(string a, string b, int expected)
    {
        Assert.Equal(expected, OpenAnswers.Distance(a, b));
        Assert.Equal(expected, OpenAnswers.Distance(b, a));
    }

    [Theory]

    // Up to 3 characters: no tolerance.
    [InlineData("Oui", "oui", OpenQuestionAnswerCategory.Accepted)]
    [InlineData("Oui", "ou", OpenQuestionAnswerCategory.Rejected)]

    // From 4 to 7: one typo.
    [InlineData("Rome", "Rome", OpenQuestionAnswerCategory.Accepted)]
    [InlineData("Rome", "Ramé", OpenQuestionAnswerCategory.ToCheck)]
    [InlineData("Rome", "Rames", OpenQuestionAnswerCategory.Rejected)]
    [InlineData("L'Italie", "itali", OpenQuestionAnswerCategory.ToCheck)]
    [InlineData("Italie", "ital", OpenQuestionAnswerCategory.Rejected)]

    // Beyond: two typos, the leading article not counting in the length.
    [InlineData("La Belgique", "belgiq", OpenQuestionAnswerCategory.ToCheck)]
    [InlineData("La Belgique", "belgi", OpenQuestionAnswerCategory.Rejected)]
    public void Classify_Answer_ToleratesTyposByTheLengthOfTheExpectedAnswer(string expected, string answer, OpenQuestionAnswerCategory category) =>
        Assert.Equal(category, OpenAnswers.Classify(OpenAnswers.Normalize(answer), new OpenQuestionDescriptor { Text = "?", Answer = expected }));

    [Theory]
    [InlineData("De Vinci", OpenQuestionAnswerCategory.Accepted)]
    [InlineData("de vinchi", OpenQuestionAnswerCategory.ToCheck)]
    [InlineData("Leonardo", OpenQuestionAnswerCategory.Rejected)]
    public void Classify_Answer_ComparesToEveryVariantToo(string answer, OpenQuestionAnswerCategory category) =>
        Assert.Equal(category, OpenAnswers.Classify(OpenAnswers.Normalize(answer), new OpenQuestionDescriptor { Text = "?", Answer = "Léonard de Vinci", AcceptedAnswers = ["De Vinci"] }));

    [Theory]
    [InlineData("1969", OpenQuestionAnswerCategory.Accepted)]
    [InlineData("1968", OpenQuestionAnswerCategory.Rejected)]
    [InlineData("19690", OpenQuestionAnswerCategory.Rejected)]
    public void Classify_NumericAnswer_HasNoTolerance(string answer, OpenQuestionAnswerCategory category) =>
        Assert.Equal(category, OpenAnswers.Classify(OpenAnswers.Normalize(answer), new OpenQuestionDescriptor { Text = "?", Answer = "1969", InputMode = OpenQuestionInputMode.Numeric }));
}
