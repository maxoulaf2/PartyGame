using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using PartyGame.Server.Packs;
using PartyGame.Tests.Shared.Audio;

namespace PartyGame.Server.Tests.Packs;

/// <summary>
/// The service of an MP3 file, executed on its own: no game mode puts one in a game yet.
/// </summary>
public sealed class AudioMediaTests : IDisposable
{
    private static readonly byte[] _audio = Mp3Samples.Constant(10);

    private readonly TempDirectory _folder = new();

    public void Dispose() => _folder.Dispose();

    public static TheoryData<string, byte[]> TaggedFiles() => new()
    {
        { "without tags", _audio },
        { "with ID3v2", [.. Mp3Samples.Id3v2("Titre secret"), .. _audio] },
        { "with ID3v1", [.. _audio, .. Mp3Samples.Id3v1("Titre secret")] },
        { "with both", [.. Mp3Samples.Id3v2("Titre secret"), .. _audio, .. Mp3Samples.Id3v1("Titre secret")] },
    };

    [Theory]
    [MemberData(nameof(TaggedFiles))]
    public async Task ServeAudio_WholeFile_ServesTheAudioOnly(string tags, byte[] content)
    {
        // When
        var (response, body) = await ServeAsync(content, range: null);

        // Then
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("audio/mpeg", response.ContentType);
        Assert.Equal(_audio.Length, response.ContentLength);
        Assert.True(_audio.AsSpan().SequenceEqual(body), $"The file {tags} is served with its tags.");
    }

    [Theory]
    [MemberData(nameof(TaggedFiles))]
    public async Task ServeAudio_Range_ServesTheRangeOfTheAudioOnly(string tags, byte[] content)
    {
        // When: the first bytes, where the ID3v2 tag would be, and a range up to the end, where the ID3v1 tag would be
        var (start, startBody) = await ServeAsync(content, "bytes=0-99");
        var (end, endBody) = await ServeAsync(content, "bytes=-100");

        // Then
        Assert.Equal((int)HttpStatusCode.PartialContent, start.StatusCode);
        Assert.Equal($"bytes 0-99/{_audio.Length}", start.Headers.ContentRange);
        Assert.True(_audio.AsSpan(0, 100).SequenceEqual(startBody), $"The start of the file {tags} is not the audio.");
        Assert.Equal($"bytes {_audio.Length - 100}-{_audio.Length - 1}/{_audio.Length}", end.Headers.ContentRange);
        Assert.True(_audio.AsSpan(_audio.Length - 100).SequenceEqual(endBody), $"The end of the file {tags} is not the audio.");
    }

    private async Task<(HttpResponse Response, byte[] Body)> ServeAsync(byte[] content, string? range)
    {
        Directory.CreateDirectory(_folder.Path);
        var file = Path.Combine(_folder.Path, $"{Guid.NewGuid():N}.mp3");
        await File.WriteAllBytesAsync(file, content, TestContext.Current.CancellationToken);

        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        context.Request.Method = HttpMethods.Get;
        if (range is not null)
        {
            context.Request.Headers[HeaderNames.Range] = range;
        }

        using var body = new MemoryStream();
        context.Response.Body = body;

        await PackMediaFiles.ServeAudio(file, "audio/mpeg", File.GetLastWriteTimeUtc(file)).ExecuteAsync(context);
        return (context.Response, body.ToArray());
    }
}
