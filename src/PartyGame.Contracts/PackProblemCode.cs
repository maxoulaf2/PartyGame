namespace PartyGame.Contracts;

/// <summary>
/// What is wrong in a pack, for the game master to fix it before the game. The client translates each code, with the
/// parameters of the <see cref="PackProblem"/>.
/// </summary>
/// <remarks>
/// Codes prefixed with <c>Pack</c> are reported by the loading of the pack, whatever the game modes. Codes prefixed with a
/// game mode are reported by that mode, which checks what the descriptor constraints cannot.
/// </remarks>
public enum PackProblemCode
{
    /// <summary>
    /// The file is not valid JSON, so nothing else could be checked. The <c>line</c> and <c>column</c> parameters, counted
    /// from 1, locate the error.
    /// </summary>
    PackJsonInvalid,

    /// <summary>
    /// A required property is absent. The path is the one of the object, and the <c>property</c> parameter the name of the
    /// absent property.
    /// </summary>
    PackPropertyMissing,

    /// <summary>
    /// A property is not part of the format, such as a misspelled one. The path is the one of the property, and the
    /// <c>property</c> parameter its name.
    /// </summary>
    PackPropertyUnknown,

    /// <summary>
    /// A value has the wrong type, or is <c>null</c> where a value is required. The <c>expected</c> parameter is one of
    /// <c>string</c>, <c>integer</c>, <c>number</c>, <c>boolean</c>, <c>array</c> or <c>object</c>.
    /// </summary>
    PackValueTypeInvalid,

    /// <summary>
    /// A number is out of its bounds. The <c>min</c> and <c>max</c> parameters are the bounds, included.
    /// </summary>
    PackValueOutOfRange,

    /// <summary>
    /// A text is too short or too long. The <c>min</c> and <c>max</c> parameters, when present, are the bounds of its
    /// number of characters, included.
    /// </summary>
    PackTextLengthOutOfRange,

    /// <summary>
    /// A list has too few or too many elements. The <c>min</c> and <c>max</c> parameters, when present, are the bounds of
    /// its number of elements, included.
    /// </summary>
    PackItemCountOutOfRange,

    /// <summary>
    /// The <c>type</c> of an activity names no game mode of this server. The path is the one of the <c>type</c> property,
    /// and the <c>type</c> parameter its value.
    /// </summary>
    PackRoundTypeUnknown,

    /// <summary>
    /// The path of a media file is not written as expected: empty, with <c>\</c> as separator, or with an empty or
    /// <c>.</c> segment or a character that some systems refuse in a file name. The <c>media</c> parameter is the path.
    /// </summary>
    PackMediaPathInvalid,

    /// <summary>
    /// The path of a media file leads out of the folder of the pack (<c>..</c> segment, absolute path). The <c>media</c>
    /// parameter is the path.
    /// </summary>
    PackMediaOutsidePack,

    /// <summary>
    /// The extension of a media file is not one its property accepts. The <c>media</c> parameter is the path, the
    /// <c>extension</c> parameter its extension, empty when it has none, and the <c>expected</c> parameter the kind of file
    /// the property accepts: <c>image</c> or <c>audio</c>.
    /// </summary>
    PackMediaTypeUnsupported,

    /// <summary>
    /// A media file does not exist in the pack. The <c>media</c> parameter is the path.
    /// </summary>
    PackMediaMissing,

    /// <summary>
    /// A media file exists, but its path differs in case from the one written in the descriptor: a case-sensitive system,
    /// such as the Raspberry Pi, would not find it. The <c>media</c> parameter is the path as written, and the
    /// <c>actual</c> parameter the path of the file.
    /// </summary>
    PackMediaCaseMismatch,

    /// <summary>
    /// An audio file has the right extension, but holds no readable MP3 audio. The <c>media</c> parameter is the path.
    /// </summary>
    PackMediaUnreadable,

    /// <summary>
    /// An audio excerpt starts at or past the end of its track. The path is the one of its <c>start</c> property, the
    /// <c>start</c> parameter its value and the <c>duration</c> parameter the duration of the track, both in seconds.
    /// </summary>
    PackAudioExcerptStartBeyondEnd,

    /// <summary>
    /// The pack could not be loaded because of an unexpected error, logged by the server. The path is <c>$</c>.
    /// </summary>
    PackLoadFailed,

    /// <summary>
    /// A zip pack is not a readable zip file. The file is the one of the archive, and the path <c>$</c>.
    /// </summary>
    PackArchiveInvalid,

    /// <summary>
    /// A zip pack holds no <c>pack.json</c>, neither at its root nor in a single folder at its root. The file is the one of
    /// the archive, and the path <c>$</c>.
    /// </summary>
    PackArchiveDescriptorMissing,

    /// <summary>
    /// An entry of a zip pack leads out of the folder it is extracted to (<c>..</c> segment, absolute path): nothing is
    /// extracted. The file is the one of the archive, the path <c>$</c>, and the <c>entry</c> parameter the entry.
    /// </summary>
    PackArchiveEntryOutside,

    /// <summary>
    /// A zip pack would take more than 2 GB once extracted: nothing is extracted. The file is the one of the archive, and
    /// the path <c>$</c>.
    /// </summary>
    PackArchiveTooLarge,

    /// <summary>
    /// A zip pack could not be extracted to the disk of the server, which is full or refuses the writing. The file is the
    /// one of the archive, and the path <c>$</c>.
    /// </summary>
    PackArchiveExtractionFailed,

    /// <summary>
    /// A folder and a zip of the pack directory, or two zips, give the same pack identifier: neither can be chosen. The
    /// file is the one of the archive, the path <c>$</c>, and the <c>id</c> parameter the identifier.
    /// </summary>
    PackIdConflict,

    /// <summary>A question of a quiz round has no correct choice. The path is the one of the question.</summary>
    QuizCorrectChoiceMissing,

    /// <summary>A question of a quiz round has several correct choices. The path is the one of the question.</summary>
    QuizCorrectChoiceDuplicated,

    /// <summary>
    /// A choice of a quiz question repeats an earlier one, ignoring case, accents and spaces. The path is the one of the
    /// repeated choice, and the <c>choice</c> parameter its text.
    /// </summary>
    QuizChoiceDuplicated,

    /// <summary>
    /// No track of a blind test round earns points: its title earns none, and its artist earns none or is not given. The
    /// round could not tell the players apart. The path is the one of the round.
    /// </summary>
    BlindTestPointsMissing,

    /// <summary>
    /// An expected answer or a variant of an open question is longer than the <c>maxLength</c> of its round, or a variant is
    /// empty: players could not type it. The path is the one of the answer, and the <c>max</c> parameter the
    /// <c>maxLength</c> of the round.
    /// </summary>
    OpenQuestionAnswerLengthOutOfRange,

    /// <summary>
    /// An expected answer or a variant of an open question normalizes to nothing, once case, accents, punctuation, spaces
    /// and a leading article are removed (« ! », « Les »): no answer could match it. The path is the one of the answer, and
    /// the <c>answer</c> parameter its text.
    /// </summary>
    OpenQuestionAnswerEmpty,

    /// <summary>
    /// A variant of an open question normalizes to the same as its expected answer or an earlier variant. The path is the
    /// one of the repeated variant, and the <c>answer</c> parameter its text.
    /// </summary>
    OpenQuestionAnswerDuplicated,

    /// <summary>
    /// An expected answer or a variant of a numeric open question holds other characters than digits. The path is the one
    /// of the answer, and the <c>answer</c> parameter its text.
    /// </summary>
    OpenQuestionAnswerNotNumeric,
}
