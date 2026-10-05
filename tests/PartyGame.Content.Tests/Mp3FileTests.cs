using PartyGame.Tests.Shared.Audio;

namespace PartyGame.Content.Tests;

public sealed class Mp3FileTests : IDisposable
{
    private readonly TestPacks _files = new();

    public void Dispose() => _files.Dispose();

    [Fact]
    public void Read_ConstantBitrate_CountsTheFrames()
    {
        // When
        var mp3 = Read(Mp3Samples.Constant(383));

        // Then: about ten seconds
        Assert.NotNull(mp3);
        Assert.Equal(383 * Mp3Samples.FrameSeconds, mp3.Duration.TotalSeconds, precision: 3);
    }

    [Fact]
    public void Read_VariableBitrateWithoutHeader_CountsFramesOfEveryLength()
    {
        // When
        var mp3 = Read(Mp3Samples.Frames(1, 9, 14, 5, 9, 11));

        // Then
        Assert.NotNull(mp3);
        Assert.Equal(6 * Mp3Samples.FrameSeconds, mp3.Duration.TotalSeconds, precision: 3);
    }

    [Fact]
    public void Read_XingHeader_TakesTheNumberOfFramesItGives()
    {
        // When: the header counts more frames than the file holds, to tell which one is read
        var mp3 = Read(Mp3Samples.WithXing(1000));

        // Then
        Assert.NotNull(mp3);
        Assert.Equal(1000 * Mp3Samples.FrameSeconds, mp3.Duration.TotalSeconds, precision: 3);
    }

    [Fact]
    public void Read_VbriHeader_TakesTheNumberOfFramesItGives()
    {
        // When
        var mp3 = Read(Mp3Samples.WithVbri(2000));

        // Then
        Assert.NotNull(mp3);
        Assert.Equal(2000 * Mp3Samples.FrameSeconds, mp3.Duration.TotalSeconds, precision: 3);
    }

    [Fact]
    public void Read_Id3Tags_LocatesTheAudioBetweenThem()
    {
        // Given
        var id3v2 = Mp3Samples.Id3v2("Titre secret");
        var audio = Mp3Samples.Constant(10);

        // When
        var mp3 = Read([.. id3v2, .. id3v2, .. audio, .. Mp3Samples.Id3v1("Titre secret")]);

        // Then: two tags in a row are both skipped, and they count for nothing in the duration
        Assert.NotNull(mp3);
        Assert.Equal(new Mp3Audio(2 * id3v2.Length, audio.Length), mp3.Audio);
        Assert.Equal(10 * Mp3Samples.FrameSeconds, mp3.Duration.TotalSeconds, precision: 3);
    }

    [Fact]
    public void Read_PaddingBeforeTheFirstFrame_FindsIt()
    {
        // When
        var mp3 = Read([.. new byte[300], .. Mp3Samples.Constant(10)]);

        // Then
        Assert.NotNull(mp3);
        Assert.Equal(10 * Mp3Samples.FrameSeconds, mp3.Duration.TotalSeconds, precision: 3);
    }

    [Theory]
    [MemberData(nameof(NotMp3))]
    public void Read_NoValidFrame_IsNull(byte[] content)
    {
        // When
        var mp3 = Read(content);

        // Then
        Assert.Null(mp3);
    }

    public static TheoryData<byte[]> NotMp3() =>
    [
        Array.Empty<byte>(),
        "Ceci n'est pas un MP3"u8.ToArray(),
        Mp3Samples.Id3v2("Titre"),

        // A lone sync pattern, not followed by a second frame
        Mp3Samples.Constant(1).Concat(new byte[500]).ToArray(),
    ];

    private Mp3File? Read(byte[] content)
    {
        Directory.CreateDirectory(_files.Root);
        var path = Path.Combine(_files.Root, $"{Guid.NewGuid():N}.mp3");
        File.WriteAllBytes(path, content);
        return Mp3File.Read(path);
    }
}
