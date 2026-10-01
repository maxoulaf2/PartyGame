using System.Text.Json;
using System.Text.Json.Serialization;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Contracts.Tests;

public sealed class TypedIdJsonConverterFactoryTests
{
    [JsonConverter(typeof(TypedIdJsonConverterFactory))]
    public readonly record struct Code(string Value);

    public readonly record struct TwoValues(Guid Value, int Other);

    public readonly record struct IntValue(int Value);

    [Fact]
    public void Serialize_StringTypedId_RoundTripsAsPlainString()
    {
        var json = JsonSerializer.Serialize(new Code("ABC"), ContractJsonOptions.Default);

        Assert.Equal("\"ABC\"", json);
        Assert.Equal(new Code("ABC"), JsonSerializer.Deserialize<Code>(json, ContractJsonOptions.Default));
    }

    [Theory]
    [InlineData(typeof(PlayerId), true)]
    [InlineData(typeof(Code), true)]
    [InlineData(typeof(TwoValues), false)]
    [InlineData(typeof(IntValue), false)]
    [InlineData(typeof(Guid), false)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(Role), false)]
    public void CanConvert_Type_AcceptsOnlyTypedIdShape(Type type, bool expected) =>
        Assert.Equal(expected, new TypedIdJsonConverterFactory().CanConvert(type));
}
