using PartyGame.Engine.Players;

namespace PartyGame.Engine.Tests.Players;

public sealed class NicknameRulesTests
{
    [Theory]
    [InlineData("Zoé", "Zoé")]
    [InlineData("  Zoé  ", "Zoé")]
    [InlineData("Jean \u00A0\u3000 Paul", "Jean Paul")]
    [InlineData("Zoe\u0301", "Zoé")]
    [InlineData("SixteenCharsLong", "SixteenCharsLong")]
    [InlineData("🎉 Max 🎉", "🎉 Max 🎉")]
    public void TryNormalize_ValidNickname_ReturnsNormalizedNickname(string text, string expected)
    {
        // When
        var valid = NicknameRules.TryNormalize(text, out var nickname);

        // Then
        Assert.True(valid);
        Assert.Equal(expected, nickname);
    }

    [Theory]
    [InlineData("\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466" + "\U0001F1EB\U0001F1F7" + "\U0001F3F4\U000E0067\U000E0062\U000E0073\U000E0063\U000E0074\U000E007F" + "\u2764\uFE0F" + "1\uFE0F\u20E3" + "\U0001F389\U0001F389\U0001F389\U0001F389\U0001F389\U0001F389\U0001F389\U0001F389\U0001F389\U0001F389\U0001F389")]
    public void TryNormalize_SixteenEmojiSequences_CountsEachAsOneCharacter(string text)
    {
        // When
        var valid = NicknameRules.TryNormalize(text, out _);

        // Then
        Assert.True(valid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SeventeenCharsLng")]
    [InlineData("Zoé\tMax")]
    [InlineData("Zoé\n")]
    [InlineData("Zoé\u0000")]
    [InlineData("Zo\u200Bé")] // zero width space
    [InlineData("\u200DZoé")] // a joiner that joins nothing
    [InlineData("Zoé\u202E")] // right-to-left override
    [InlineData("Zoé\u2028Max")] // line separator
    [InlineData("\u3164")] // hangul filler
    [InlineData("\u2800\u2800")] // braille blank
    [InlineData("\u0301")] // combining accent alone
    public void TryNormalize_InvalidNickname_ReturnsFalse(string text)
    {
        // When
        var valid = NicknameRules.TryNormalize(text, out var nickname);

        // Then
        Assert.False(valid);
        Assert.Null(nickname);
    }

    [Fact]
    public void TryNormalize_LoneSurrogate_ReturnsFalse()
    {
        // Given: built in code, since attribute arguments are stored as UTF-8, which cannot hold a lone surrogate
        var text = "Zo" + (char)0xD800 + "é";

        // When
        var valid = NicknameRules.TryNormalize(text, out _);

        // Then
        Assert.False(valid);
    }

    [Theory]
    [InlineData("Zoé", "zoe")]
    [InlineData("ZOÉ", "zoé")]
    [InlineData("Ærøskøbing", "ærøskøbing")]
    [InlineData("\u2764\uFE0F", "\u2764")]
    public void AreSame_SameNicknameIgnoringCaseAndAccents_ReturnsTrue(string first, string second) =>
        Assert.True(NicknameRules.AreSame(first, second));

    [Theory]
    [InlineData("Zoe", "Zoey")]
    [InlineData("Jean Paul", "JeanPaul")]
    public void AreSame_DifferentNicknames_ReturnsFalse(string first, string second) =>
        Assert.False(NicknameRules.AreSame(first, second));
}
