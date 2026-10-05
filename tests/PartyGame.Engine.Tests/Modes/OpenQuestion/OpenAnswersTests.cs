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
}
