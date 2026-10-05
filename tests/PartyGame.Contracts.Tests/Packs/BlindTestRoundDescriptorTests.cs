using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PartyGame.Contracts.Packs;

namespace PartyGame.Contracts.Tests.Packs;

public sealed class BlindTestRoundDescriptorTests
{
    [Fact]
    public void Deserialize_CompleteBlindTestRound_ReadsEveryProperty()
    {
        var blindTest = ReadRound("""{"type":"blindtest","title":"Les classiques","titlePoints":300,"artistPoints":200,"tracks":[{"excerpt":{"file":"sons/ode.mp3","start":12.5,"duration":20},"title":"L'Hymne à la joie","artist":"Beethoven","image":"images/beethoven.png"}]}""");

        Assert.Equal("Les classiques", blindTest.Title);
        Assert.Equal(300, blindTest.TitlePoints);
        Assert.Equal(200, blindTest.ArtistPoints);
        var track = Assert.Single(blindTest.Tracks);
        Assert.Equal(new AudioExcerpt { File = new MediaPath("sons/ode.mp3"), Start = 12.5, Duration = 20 }, track.Excerpt);
        Assert.Equal("L'Hymne à la joie", track.Title);
        Assert.Equal("Beethoven", track.Artist);
        Assert.Equal(new MediaPath("images/beethoven.png"), track.Image);
    }

    [Fact]
    public void Deserialize_BlindTestRoundWithoutOptionalProperties_UsesDefaults()
    {
        var blindTest = ReadRound("""{"type":"blindtest","title":"Manche 1","tracks":[{"excerpt":{"file":"ode.mp3","duration":20},"title":"Titre"}]}""");

        Assert.Equal(500, blindTest.TitlePoints);
        Assert.Equal(500, blindTest.ArtistPoints);
        var track = Assert.Single(blindTest.Tracks);
        Assert.Null(track.Artist);
        Assert.Null(track.Image);
    }

    [Theory]
    [InlineData("""{"type":"blindtest","title":"Manche 1"}""")]
    [InlineData("""{"type":"blindtest","title":"Manche 1","tracks":[{"title":"Titre"}]}""")]
    [InlineData("""{"type":"blindtest","title":"Manche 1","tracks":[{"excerpt":{"file":"ode.mp3","duration":20}}]}""")]
    [InlineData("""{"type":"blindtest","title":"Manche 1","tracks":[],"titlePoints":"500"}""")]
    [InlineData("""{"type":"blindtest","title":"Manche 1","tracks":[{"excerpt":"ode.mp3","title":"Titre"}]}""")]
    public void Deserialize_MissingOrMistypedProperty_Throws(string round) =>
        Assert.Throws<JsonException>(() => ReadRound(round));

    [Theory]
    [InlineData("""{"type":"blindtest","title":"Manche 1","tracks":[],"points":500}""")]
    [InlineData("""{"type":"blindtest","title":"Manche 1","tracks":[{"excerpt":{"file":"ode.mp3","duration":20},"title":"Titre","answer":"Titre"}]}""")]
    public void Deserialize_UnknownProperty_Throws(string round) =>
        Assert.Throws<JsonException>(() => ReadRound(round));

    [Fact]
    public void Validate_ValidRoundAndTrack_ReportNothing()
    {
        Assert.Empty(Validate(Round()));
        Assert.Empty(Validate(Track() with { Artist = "Beethoven" }));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void Validate_PointsOutOfBounds_ReportsThem(int points) =>
        Assert.Equal(
            [nameof(BlindTestRoundDescriptor.TitlePoints), nameof(BlindTestRoundDescriptor.ArtistPoints)],
            Validate(Round() with { TitlePoints = points, ArtistPoints = points }));

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_TrackCountOutOfBounds_ReportsTracks(int count) =>
        Assert.Equal([nameof(BlindTestRoundDescriptor.Tracks)], Validate(Round() with { Tracks = [.. Enumerable.Repeat(Track(), count)] }));

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_TitleAndArtistLengthOutOfBounds_ReportThem(int length) =>
        Assert.Equal(
            [nameof(BlindTestTrack.Title), nameof(BlindTestTrack.Artist)],
            Validate(Track() with { Title = new string('a', length), Artist = new string('a', length) }));

    private static BlindTestRoundDescriptor ReadRound(string round) =>
        Assert.IsType<BlindTestRoundDescriptor>(Assert.Single(
            (JsonSerializer.Deserialize<PackDescriptor>($$"""{"formatVersion":1,"title":"Soirée","rounds":[{{round}}]}""", PackJsonOptions.Default)
             ?? throw new InvalidOperationException("The descriptor is null.")).Rounds));

    private static BlindTestRoundDescriptor Round() => new() { Title = "Manche 1", Tracks = [Track()] };

    private static BlindTestTrack Track() =>
        new() { Excerpt = new AudioExcerpt { File = new MediaPath("ode.mp3"), Duration = 20 }, Title = "L'Hymne à la joie" };

    // The same attributes produce the schema constraints. The validator checks one object, not the objects it contains.
    private static List<string> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames)];
    }
}
