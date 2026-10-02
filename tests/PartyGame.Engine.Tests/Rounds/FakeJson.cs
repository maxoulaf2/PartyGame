using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// The wire conventions, plus the activities and views of <see cref="FakeMode"/>, which <c>PartyGame.Contracts</c> does
/// not declare, so that the tests serialize the snapshots of a round as the hub would, and the state as it is persisted.
/// </summary>
internal static class FakeJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static string Serialize(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(ContractJsonOptions.Default)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { AddFakeTypes } },
        };
        options.MakeReadOnly();
        return options;
    }

    private static void AddFakeTypes(JsonTypeInfo typeInfo)
    {
        Type? fake = typeInfo.Type switch
        {
            var t when t == typeof(PlayerRoundView) => typeof(FakePlayerView),
            var t when t == typeof(DisplayRoundView) => typeof(FakeDisplayView),
            var t when t == typeof(GameMasterRoundView) => typeof(FakeGameMasterView),
            var t when t == typeof(RoundDescriptor) => typeof(FakeRoundDescriptor),
            _ => null,
        };

        if (fake is not null)
        {
            typeInfo.PolymorphismOptions!.DerivedTypes.Add(new JsonDerivedType(fake, "fake"));
        }
    }
}
