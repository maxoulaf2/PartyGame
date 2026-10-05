namespace PartyGame.Content;

/// <summary>
/// Where the audio of an MP3 file lies: after its ID3v2 tags, before its ID3v1 tag. The tags hold the title, the artist
/// and the cover of the track, which the server never serves: they would give away the answer of a blind test.
/// </summary>
/// <param name="Start">The offset of the first byte of audio.</param>
/// <param name="Length">The number of bytes of audio.</param>
public readonly record struct Mp3Audio(long Start, long Length)
{
    private const int Id3v1Length = 128;
    private const int Id3v2HeaderLength = 10;

    /// <summary>
    /// Finds the audio of an MP3 file by its tags only: a few bytes read at each end.
    /// </summary>
    /// <param name="stream">The file, readable and seekable.</param>
    public static Mp3Audio Find(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var length = stream.Length;
        var start = 0L;
        var header = new byte[Id3v2HeaderLength];

        // Some taggers write several tags in a row.
        while (start + Id3v2HeaderLength <= length && Id3v2Length(Read(stream, start, header)) is > 0 and var size)
        {
            start = Math.Min(start + size, length);
        }

        var end = length;
        var tail = new byte[Id3v1Length];
        if (end - start >= Id3v1Length && Read(stream, end - Id3v1Length, tail).StartsWith("TAG"u8))
        {
            end -= Id3v1Length;
        }

        return new Mp3Audio(start, end - start);
    }

    private static ReadOnlySpan<byte> Read(Stream stream, long position, byte[] buffer)
    {
        stream.Position = position;
        stream.ReadExactly(buffer);
        return buffer;
    }

    // The length of the ID3v2 tag that starts with this header, footer included, or 0 when it starts no tag.
    private static long Id3v2Length(ReadOnlySpan<byte> header)
    {
        // The size is a "synchsafe" integer: 7 bits per byte, the high bit always clear.
        if (!header.StartsWith("ID3"u8) || (header[6] | header[7] | header[8] | header[9]) >= 0x80)
        {
            return 0;
        }

        const int FooterFlag = 0x10;
        var size = (header[6] << 21) | (header[7] << 14) | (header[8] << 7) | header[9];
        return Id3v2HeaderLength + size + ((header[5] & FooterFlag) != 0 ? Id3v2HeaderLength : 0);
    }
}
