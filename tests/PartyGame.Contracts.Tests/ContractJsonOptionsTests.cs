using System.Collections.Immutable;
using System.Text.Json;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Contracts.Tests;

public sealed class ContractJsonOptionsTests
{
    private static readonly Guid _guid = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

    public sealed record Sample(PlayerId Player, PlayerId? Previous, Role Role, string? Nickname);

    public sealed record Scores(ImmutableDictionary<PlayerId, int> ByPlayer);

    [Fact]
    public void Serialize_Record_UsesCamelCaseStringEnumsAndExplicitNulls()
    {
        var json = JsonSerializer.Serialize(new Sample(new PlayerId(_guid), null, Role.GameMaster, null), ContractJsonOptions.Default);

        Assert.Equal(
            """{"player":"0f8fad5b-d9cb-469f-a165-70867728950e","previous":null,"role":"GameMaster","nickname":null}""",
            json);
    }

    [Fact]
    public void Deserialize_Record_RoundTrips()
    {
        var sample = new Sample(new PlayerId(_guid), new PlayerId(Guid.Empty), Role.Display, "Alice");

        var roundTripped = JsonSerializer.Deserialize<Sample>(JsonSerializer.Serialize(sample, ContractJsonOptions.Default), ContractJsonOptions.Default);

        Assert.Equal(sample, roundTripped);
    }

    [Fact]
    public void Serialize_TypedIdDictionaryKey_WritesPlainString()
    {
        var scores = new Scores(ImmutableDictionary<PlayerId, int>.Empty.Add(new PlayerId(_guid), 3));

        var json = JsonSerializer.Serialize(scores, ContractJsonOptions.Default);

        Assert.Equal("""{"byPlayer":{"0f8fad5b-d9cb-469f-a165-70867728950e":3}}""", json);
        Assert.Equal(scores.ByPlayer, JsonSerializer.Deserialize<Scores>(json, ContractJsonOptions.Default)!.ByPlayer);
    }

    [Fact]
    public void Serialize_TypedIdWithoutContractOptions_StillWritesPlainString()
    {
        // The converter is attached to the type, so serializers configured elsewhere agree on the wire format.
        Assert.Equal("\"0f8fad5b-d9cb-469f-a165-70867728950e\"", JsonSerializer.Serialize(new PlayerId(_guid)));
    }

    [Theory]
    [InlineData("""{"player":"not-a-guid","previous":null,"role":"Player","nickname":null}""")]
    [InlineData("""{"player":42,"previous":null,"role":"Player","nickname":null}""")]
    [InlineData("""{"player":null,"previous":null,"role":"Player","nickname":null}""")]
    [InlineData("""{"player":"0f8fad5b-d9cb-469f-a165-70867728950e","previous":null,"role":1,"nickname":null}""")]
    [InlineData("""{"player":"0f8fad5b-d9cb-469f-a165-70867728950e","previous":null,"role":"Referee","nickname":null}""")]
    [InlineData("""{"previous":null,"role":"Player","nickname":null}""")]
    public void Deserialize_MalformedJson_Throws(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Sample>(json, ContractJsonOptions.Default));

    [Fact]
    public void Apply_ExistingOptions_ConfiguresWireConventions()
    {
        var options = new JsonSerializerOptions();

        ContractJsonOptions.Apply(options);

        Assert.Equal(
            JsonSerializer.Serialize(new Sample(new PlayerId(_guid), null, Role.Player, "Bob"), ContractJsonOptions.Default),
            JsonSerializer.Serialize(new Sample(new PlayerId(_guid), null, Role.Player, "Bob"), options));
    }
}
