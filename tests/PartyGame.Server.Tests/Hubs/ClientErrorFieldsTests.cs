using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class ClientErrorFieldsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData("abcd", "abcd")]
    [InlineData("abcdef", "abcd")]
    public void Truncate_Value_KeepsAtMostTheMaximumLength(string? value, string? expected)
    {
        // When
        var truncated = ClientErrorFields.Truncate(value, maxLength: 4);

        // Then
        Assert.Equal(expected, truncated);
    }

    [Fact]
    public void Truncate_CutInASurrogatePair_DropsTheWholePair()
    {
        // Given: the high half of the surrogate pair is the fourth character.
        const string Value = "abc🎉d";

        // When
        var truncated = ClientErrorFields.Truncate(Value, maxLength: 4);

        // Then
        Assert.Equal("abc", truncated);
    }
}
