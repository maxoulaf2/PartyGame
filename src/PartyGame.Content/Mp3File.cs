using System.Buffers.Binary;

namespace PartyGame.Content;

/// <summary>
/// What the server needs to know of an MP3 file: the duration of its track, and where its audio lies between the ID3 tags.
/// </summary>
/// <remarks>
/// Only MPEG audio layer III is read, the format browsers play as <c>audio/mpeg</c>. The duration comes from the Xing,
/// Info or VBRI header that encoders write in the first frame, or else from the headers of every frame.
/// </remarks>
/// <param name="Duration">The duration of the track.</param>
/// <param name="Audio">The audio data, without the ID3 tags.</param>
public sealed record Mp3File(TimeSpan Duration, Mp3Audio Audio)
{
    // Where to look for the first frame after the tags: encoders write it right there, past some padding at most.
    private const int SyncSearchLength = 64 * 1024;

    private static readonly int[] _mpeg1Bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];
    private static readonly int[] _mpeg2Bitrates = [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160];
    private static readonly int[] _mpeg1SampleRates = [44100, 48000, 32000];

    /// <summary>
    /// Reads an MP3 file.
    /// </summary>
    /// <returns>The file, or <see langword="null"/> when it holds no valid MP3 frame.</returns>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static Mp3File? Read(string path)
    {
        using var stream = File.OpenRead(path);
        var audio = Mp3Audio.Find(stream);

        var head = new byte[(int)Math.Min(audio.Length, SyncSearchLength)];
        stream.Position = audio.Start;
        stream.ReadExactly(head);
        if (FirstFrame(head, wholeAudio: head.Length == audio.Length) is not { } first)
        {
            return null;
        }

        var header = Header(head.AsSpan(first))!.Value;
        if (FrameCount(head.AsSpan(first), header) is { } frames)
        {
            return new Mp3File(header.Duration(frames), audio);
        }

        var data = new byte[audio.Length];
        stream.Position = audio.Start;
        stream.ReadExactly(data);
        return new Mp3File(header.Duration(CountFrames(data, first)), audio);
    }

    // The first frame header followed by another one, or by the end of the audio: a lone sync pattern is too often a
    // false one, in the padding or in a broken file.
    private static int? FirstFrame(ReadOnlySpan<byte> data, bool wholeAudio)
    {
        for (var position = 0; position + 4 <= data.Length; position++)
        {
            if (Header(data[position..]) is { } header
                && (Header(data[Math.Min(position + header.Length, data.Length)..]) is not null
                    || (wholeAudio && position + header.Length == data.Length)))
            {
                return position;
            }
        }

        return null;
    }

    // The number of audio frames that the Xing, Info or VBRI header of the first frame gives, if it has one.
    private static long? FrameCount(ReadOnlySpan<byte> frame, FrameHeader header)
    {
        var xing = 4 + header.SideInfoLength;
        if (frame.Length >= xing + 12 && (frame[xing..].StartsWith("Xing"u8) || frame[xing..].StartsWith("Info"u8)))
        {
            const int FramesFlag = 1;
            var flags = BinaryPrimitives.ReadInt32BigEndian(frame[(xing + 4)..]);
            return (flags & FramesFlag) != 0 ? BinaryPrimitives.ReadUInt32BigEndian(frame[(xing + 8)..]) : null;
        }

        const int Vbri = 4 + 32;
        if (frame.Length >= Vbri + 18 && frame[Vbri..].StartsWith("VBRI"u8))
        {
            return BinaryPrimitives.ReadUInt32BigEndian(frame[(Vbri + 14)..]);
        }

        return null;
    }

    // Walks the frames from the first one, up to the end of the audio or to whatever is not a frame.
    private static long CountFrames(ReadOnlySpan<byte> data, int position)
    {
        var frames = 0L;
        while (Header(data[position..]) is { } header && position + header.Length <= data.Length)
        {
            frames++;
            position += header.Length;
        }

        return frames;
    }

    private static FrameHeader? Header(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xFF || (bytes[1] & 0xE0) != 0xE0)
        {
            return null;
        }

        var version = (bytes[1] >> 3) & 3; // 0: MPEG 2.5, 1: reserved, 2: MPEG 2, 3: MPEG 1
        var layer = (bytes[1] >> 1) & 3; // 1: layer III
        var bitrateIndex = bytes[2] >> 4;
        var sampleRateIndex = (bytes[2] >> 2) & 3;
        if (version == 1 || layer != 1 || bitrateIndex is 0 or 15 || sampleRateIndex == 3)
        {
            return null;
        }

        var mpeg1 = version == 3;
        var bitrate = (mpeg1 ? _mpeg1Bitrates : _mpeg2Bitrates)[bitrateIndex] * 1000;
        var sampleRate = _mpeg1SampleRates[sampleRateIndex] >> version switch { 3 => 0, 2 => 1, _ => 2 };
        var samplesPerFrame = mpeg1 ? 1152 : 576;
        var padding = (bytes[2] >> 1) & 1;
        var mono = bytes[3] >> 6 == 3;
        return new FrameHeader(
            Length: (samplesPerFrame / 8 * bitrate / sampleRate) + padding,
            sampleRate,
            samplesPerFrame,
            SideInfoLength: (mpeg1, mono) switch
            {
                (true, false) => 32,
                (true, true) or (false, false) => 17,
                (false, true) => 9,
            });
    }

    private readonly record struct FrameHeader(int Length, int SampleRate, int SamplesPerFrame, int SideInfoLength)
    {
        public TimeSpan Duration(long frames) => TimeSpan.FromSeconds((double)frames * SamplesPerFrame / SampleRate);
    }
}
