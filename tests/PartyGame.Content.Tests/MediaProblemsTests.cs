using static PartyGame.Content.Tests.TestPacks;

namespace PartyGame.Content.Tests;

public sealed class MediaProblemsTests : IDisposable
{
    private const string ImagePath = "$.rounds[0].questions[0].image";

    private readonly TestPacks _packs = new();

    public void Dispose() => _packs.Dispose();

    [Theory]
    [InlineData("images/photo.png", "images/photo.png")]
    [InlineData("photo.jpg", "photo.jpg")]
    [InlineData("images/vacances/photo.jpeg", "images/vacances/photo.jpeg")]
    [InlineData("images/photo.WEBP", "images/photo.WEBP")]
    public void Load_MediaPresentWithTheSameCase_IsValid(string image, string file)
    {
        // When
        var pack = _packs.Load(Pack(Quiz(QuestionWithImage(image))), file);

        // Then
        Assert.True(pack.IsValid);
    }

    [Theory]
    [InlineData("images/absente.png")]
    [InlineData("autres/photo.png")]
    [InlineData("images/photo.png/photo.png")]
    public void Load_MediaMissing_IsReported(string image)
    {
        // When
        var pack = _packs.Load(Pack(Quiz(QuestionWithImage(image))), "images/photo.png");

        // Then
        Assert.Equal([$"PackMediaMissing pack.json {ImagePath} media={image}"], Describe(pack));
    }

    [Theory]
    [InlineData("../photo.png")]
    [InlineData("images/../../photo.png")]
    [InlineData("/photo.png")]
    [InlineData("C:/photo.png")]
    [InlineData("c:photo.png")]
    [InlineData("\\photo.png")]
    [InlineData("..\\photo.png")]
    public void Load_MediaOutsideThePack_IsReported(string image)
    {
        // Given
        _packs.Add("other", Pack(Quiz(ValidQuestion)), "photo.png");

        // When
        var pack = _packs.Load(Pack(Quiz(QuestionWithImage(image))), "photo.png");

        // Then
        Assert.Equal([$"PackMediaOutsidePack pack.json {ImagePath} media={image}"], Describe(pack));
    }

    [Theory]
    [InlineData("")]
    [InlineData("images\\photo.png")]
    [InlineData("images//photo.png")]
    [InlineData("./images/photo.png")]
    [InlineData("images/")]
    [InlineData("images/photo?.png")]
    public void Load_MediaPathWrittenWrong_IsReported(string image)
    {
        // When
        var pack = _packs.Load(Pack(Quiz(QuestionWithImage(image))), "images/photo.png");

        // Then
        Assert.Equal([$"PackMediaPathInvalid pack.json {ImagePath} media={image}"], Describe(pack));
    }

    [Theory]
    [InlineData("images/anim.gif", ".gif")]
    [InlineData("images/photo", "")]
    [InlineData("sons/extrait.mp3", ".mp3")]
    public void Load_MediaOfAnUnsupportedType_IsReported(string image, string extension)
    {
        // When
        var pack = _packs.Load(Pack(Quiz(QuestionWithImage(image))), image);

        // Then
        Assert.Equal([$"PackMediaTypeUnsupported pack.json {ImagePath} extension={extension} media={image}"], Describe(pack));
    }

    [Fact]
    public void Load_MediaOfAnUnsupportedTypeAndMissing_ReportsBoth()
    {
        // When
        var pack = _packs.Load(Pack(Quiz(QuestionWithImage("anim.gif"))));

        // Then
        Assert.Equal(
            [
                $"PackMediaTypeUnsupported pack.json {ImagePath} extension=.gif media=anim.gif",
                $"PackMediaMissing pack.json {ImagePath} media=anim.gif",
            ],
            Describe(pack));
    }

    [Theory]
    [InlineData("images/Photo.png")]
    [InlineData("images/photo.PNG")]
    [InlineData("Images/photo.png")]
    public void Load_MediaWithAnotherCase_IsReportedWithThePathOfTheFile(string image)
    {
        // When
        var pack = _packs.Load(Pack(Quiz(QuestionWithImage(image))), "images/photo.png");

        // Then
        Assert.Equal([$"PackMediaCaseMismatch pack.json {ImagePath} actual=images/photo.png media={image}"], Describe(pack));
    }

    [Fact]
    public void Load_SeveralMedia_ChecksEachOne()
    {
        // Given
        var round = Quiz(QuestionWithImage("a.png"), QuestionWithImage("b.png"), QuestionWithImage("a.png"));

        // When
        var pack = _packs.Load(Pack(round), "a.png");

        // Then
        Assert.Equal(["PackMediaMissing pack.json $.rounds[0].questions[1].image media=b.png"], Describe(pack));
    }
}
