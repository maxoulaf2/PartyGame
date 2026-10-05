using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes.BlindTest;

namespace PartyGame.Engine.Tests.Modes.BlindTest;

public sealed class BlindTestModeTests
{
    private static readonly BlindTestMode _mode = new();

    private static readonly BlindTestTrack _track = new()
    {
        Excerpt = new AudioExcerpt { File = new MediaPath("ode.mp3"), Duration = 20 },
        Title = "L'Hymne à la joie",
    };

    private static readonly BlindTestRoundDescriptor _round = new() { Title = "Les classiques", Tracks = [_track] };

    [Fact]
    public void Validate_RoundWhoseTitlesEarnPoints_ReportsNothing() =>
        Assert.Empty(_mode.Validate(_round with { ArtistPoints = 0 }, "$.rounds[0]"));

    [Fact]
    public void Validate_RoundWhoseArtistsAloneEarnPoints_ReportsNothing() =>
        Assert.Empty(_mode.Validate(_round with { TitlePoints = 0, Tracks = [_track, _track with { Artist = "Beethoven" }] }, "$.rounds[0]"));

    [Theory]
    [InlineData(0, 0, "Beethoven")]
    [InlineData(0, 500, null)]
    public void Validate_RoundWhereNoTrackEarnsPoints_ReportsMissingPoints(int titlePoints, int artistPoints, string? artist)
    {
        // When
        var problems = _mode.Validate(
            _round with { TitlePoints = titlePoints, ArtistPoints = artistPoints, Tracks = [_track with { Artist = artist }] },
            "$.rounds[1]");

        // Then
        var problem = Assert.Single(problems);
        Assert.Equal(PackProblemCode.BlindTestPointsMissing, problem.Code);
        Assert.Equal(PackDescriptor.FileName, problem.File);
        Assert.Equal("$.rounds[1]", problem.Path);
        Assert.Empty(problem.Parameters);
    }

    [Fact]
    public void Start_UntilTheTracksArePlayed_FinishesTheRound()
    {
        // When
        var transition = _mode.Start(_round, Games.NewLobby(), Games.Context(42));

        // Then
        Assert.True(transition.IsFinished);
        Assert.IsType<BlindTestRound>(transition.State);
        Assert.Empty(transition.Effects);
    }
}
