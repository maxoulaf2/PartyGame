using System.Buffers.Binary;
using System.Text;

namespace PartyGame.Tests.Shared.Audio;

/// <summary>
/// Builds MP3 files of silent frames, which browsers play as silence: MPEG-1 layer III, 44.1 kHz, stereo, without CRC.
/// </summary>
internal static class Mp3Samples
{
    /// <summary>
    /// The samples of a frame at 44.1 kHz.
    /// </summary>
    public const double FrameSeconds = 1152 / 44100d;

    /// <summary>
    /// 128 kbps.
    /// </summary>
    public const int DefaultBitrateIndex = 9;

    /// <summary>
    /// A file of frames, one per bitrate index (1 to 14, 32 to 320 kbps).
    /// </summary>
    public static byte[] Frames(params int[] bitrateIndices) => [.. bitrateIndices.SelectMany(Frame)];

    /// <summary>
    /// A file of frames at a constant bitrate.
    /// </summary>
    public static byte[] Constant(int frames, int bitrateIndex = DefaultBitrateIndex) =>
        Frames([.. Enumerable.Repeat(bitrateIndex, frames)]);

    /// <summary>
    /// A first frame whose Xing header gives a number of frames, as LAME writes it, followed by a few frames.
    /// </summary>
    public static byte[] WithXing(uint frames)
    {
        var first = Frame(DefaultBitrateIndex);
        "Xing"u8.CopyTo(first.AsSpan(4 + 32));
        BinaryPrimitives.WriteInt32BigEndian(first.AsSpan(4 + 32 + 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(first.AsSpan(4 + 32 + 8), frames);
        return [.. first, .. Constant(3)];
    }

    /// <summary>
    /// A first frame whose VBRI header gives a number of frames, as the Fraunhofer encoder writes it, followed by a few frames.
    /// </summary>
    public static byte[] WithVbri(uint frames)
    {
        var first = Frame(DefaultBitrateIndex);
        "VBRI"u8.CopyTo(first.AsSpan(4 + 32));
        BinaryPrimitives.WriteUInt32BigEndian(first.AsSpan(4 + 32 + 14), frames);
        return [.. first, .. Constant(3)];
    }

    /// <summary>
    /// An ID3v2.3 tag with a title frame.
    /// </summary>
    public static byte[] Id3v2(string title)
    {
        byte[] text = [0, .. Encoding.Latin1.GetBytes(title)];
        byte[] frame = [.. "TIT2"u8, 0, 0, 0, (byte)text.Length, 0, 0, .. text];
        var size = frame.Length;
        return [.. "ID3"u8, 3, 0, 0, (byte)(size >> 21 & 0x7F), (byte)(size >> 14 & 0x7F), (byte)(size >> 7 & 0x7F), (byte)(size & 0x7F), .. frame];
    }

    /// <summary>
    /// An ID3v1 tag with a title, written at the end of a file.
    /// </summary>
    public static byte[] Id3v1(string title)
    {
        var tag = new byte[128];
        "TAG"u8.CopyTo(tag);
        Encoding.Latin1.GetBytes(title).CopyTo(tag, 3);
        return tag;
    }

    private static byte[] Frame(int bitrateIndex)
    {
        int[] bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];
        var frame = new byte[144 * bitrates[bitrateIndex] * 1000 / 44100];
        frame[0] = 0xFF;
        frame[1] = 0xFB;
        frame[2] = (byte)(bitrateIndex << 4);
        return frame;
    }
}
