using System.Globalization;
using System.Text.Json;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.BlindTest;
using PartyGame.Tests.Shared.Audio;
using static PartyGame.Content.Tests.TestPacks;

namespace PartyGame.Content.Tests;

/// <summary>
/// The checks of an audio excerpt, read on its own, then within a blind test round.
/// </summary>
public sealed class AudioExcerptProblemsTests : IDisposable
{
    // A track of about ten seconds.
    private static readonly byte[] _track = Mp3Samples.Constant(383);

    private readonly TestPacks _packs = new();

    public void Dispose() => _packs.Dispose();

    [Theory]
    [InlineData("""{ "file": "sons/morceau.mp3", "duration": 30 }""")]
    [InlineData("""{ "file": "sons/morceau.mp3", "start": 5.5, "duration": 5 }""")]
    [InlineData("""{ "file": "sons/morceau.mp3", "start": 9.9, "duration": 120 }""")]
    public void Check_ExcerptThatStartsBeforeTheEnd_IsValid(string excerpt)
    {
        // When: an excerpt that may run past the end of the track
        var problems = Check(excerpt, ("sons/morceau.mp3", _track));

        // Then
        Assert.Empty(problems);
    }

    [Theory]
    [InlineData(10.1)]
    [InlineData(60)]
    public void Check_ExcerptThatStartsPastTheEnd_IsReported(double start)
    {
        // When
        var problems = Check(
            $$"""{ "file": "morceau.mp3", "start": {{start.ToString(CultureInfo.InvariantCulture)}}, "duration": 30 }""",
            ("morceau.mp3", _track));

        // Then
        Assert.Equal([FormattableString.Invariant($"PackAudioExcerptStartBeyondEnd pack.json $.start duration=10 start={start}")], problems);
    }

    [Theory]
    [InlineData("""{ "file": "morceau.mp3", "start": -1, "duration": 30 }""", "PackValueOutOfRange pack.json $.start max=3600 min=0")]
    [InlineData("""{ "file": "morceau.mp3", "duration": 4 }""", "PackValueOutOfRange pack.json $.duration max=120 min=5")]
    [InlineData("""{ "file": "morceau.mp3", "duration": 121 }""", "PackValueOutOfRange pack.json $.duration max=120 min=5")]
    [InlineData("""{ "file": "morceau.mp3", "duration": 12.5 }""", "PackValueTypeInvalid pack.json $.duration expected=integer")]
    [InlineData("""{ "file": "morceau.mp3" }""", "PackPropertyMissing pack.json $ property=duration")]
    [InlineData("""{ "duration": 30 }""", "PackPropertyMissing pack.json $ property=file")]
    public void Check_ExcerptOutOfItsBounds_IsReported(string excerpt, string problem)
    {
        // When
        var problems = Check(excerpt, ("morceau.mp3", _track));

        // Then
        Assert.Equal([problem], problems);
    }

    [Theory]
    [InlineData("pochette.png", ".png")]
    [InlineData("morceau.ogg", ".ogg")]
    [InlineData("morceau", "")]
    public void Check_FileThatIsNotAnMp3_IsReported(string file, string extension)
    {
        // When
        var problems = Check($$"""{ "file": "{{file}}", "duration": 30 }""", (file, _track));

        // Then
        Assert.Equal([$"PackMediaTypeUnsupported pack.json $.file expected=audio extension={extension} media={file}"], problems);
    }

    [Fact]
    public void Check_Mp3WithoutAnyFrame_IsReportedUnreadable()
    {
        // When
        var problems = Check("""{ "file": "morceau.mp3", "start": 50, "duration": 30 }""", ("morceau.mp3", "pas du son"u8.ToArray()));

        // Then: an excerpt of an unreadable file is not checked against its duration
        Assert.Equal(["PackMediaUnreadable pack.json $.file media=morceau.mp3"], problems);
    }

    [Fact]
    public void Check_Mp3Missing_IsReportedMissingOnly()
    {
        // When
        var problems = Check("""{ "file": "morceau.mp3", "duration": 30 }""");

        // Then
        Assert.Equal(["PackMediaMissing pack.json $.file media=morceau.mp3"], problems);
    }

    [Fact]
    public void Load_BlindTestRoundWithInvalidTracks_ReportsEachProblemAtItsPathInTheDescriptor()
    {
        // Given: a track that starts past the end of its file, then one whose file and image are missing
        var folder = _packs.Add("pack", Pack("""
            { "type": "blindtest", "title": "Manche", "tracks": [
                { "excerpt": { "file": "sons/ode.mp3", "start": 15, "duration": 20 }, "title": "Ode" },
                { "excerpt": { "file": "sons/absent.mp3", "duration": 20 }, "title": "Absent", "image": "images/absent.png" }
            ] }
            """));
        Directory.CreateDirectory(Path.Combine(folder, "sons"));
        File.WriteAllBytes(Path.Combine(folder, "sons", "ode.mp3"), _track);

        // When
        var pack = new PackLoader(new GameModes([new BlindTestMode()]).Validate).Load(folder);

        // Then
        Assert.False(pack.IsValid);
        Assert.Equal(
            [
                "PackAudioExcerptStartBeyondEnd pack.json $.rounds[0].tracks[0].excerpt.start duration=10 start=15",
                "PackMediaMissing pack.json $.rounds[0].tracks[1].excerpt.file media=sons/absent.mp3",
                "PackMediaMissing pack.json $.rounds[0].tracks[1].image media=images/absent.png",
            ],
            Describe(pack).Order(StringComparer.Ordinal));
    }

    private List<string> Check(string excerpt, params (string Path, byte[] Content)[] files)
    {
        var folder = _packs.Add("pack", "{}", [.. files.Select(file => file.Path)]);
        foreach (var (path, content) in files)
        {
            File.WriteAllBytes(Path.Combine(folder, path.Replace('/', Path.DirectorySeparatorChar)), content);
        }

        using var document = JsonDocument.Parse(excerpt);
        var reading = DescriptorReader.Read(document.RootElement, typeof(AudioExcerpt));
        return [.. reading.Problems.Concat(MediaCheck.Check(folder, reading)).Select(Describe)];
    }
}
