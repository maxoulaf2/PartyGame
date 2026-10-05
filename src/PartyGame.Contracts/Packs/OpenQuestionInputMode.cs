using System.Text.Json.Serialization;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// The keyboard the phones show to type the answer to an open question.
/// </summary>
public enum OpenQuestionInputMode
{
    /// <summary>
    /// A text keyboard. The answers are compared with a tolerance for typos.
    /// </summary>
    [JsonStringEnumMemberName("text")]
    Text,

    /// <summary>
    /// A numeric keyboard: the expected answers are whole numbers written with digits only, compared without tolerance.
    /// </summary>
    [JsonStringEnumMemberName("numeric")]
    Numeric,
}
