namespace PartyGame.Contracts.Packs;

/// <summary>
/// Marks a <see cref="MediaPath"/> property that references an audio file: the loading of the pack accepts only MP3 files
/// there, and checks that they are readable. A media path without it references an image.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AudioFileAttribute : Attribute;
