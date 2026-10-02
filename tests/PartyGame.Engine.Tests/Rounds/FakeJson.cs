using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// The wire conventions, plus the views of <see cref="FakeMode"/>, which <c>PartyGame.Contracts</c> does not declare, so
/// that the tests serialize the snapshots of a round as the hub would.
/// </summary>
internal static class FakeJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static string Serialize(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(ContractJsonOptions.Default)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { AddFakeViews } },
        };
        options.MakeReadOnly();
        return options;
    }

    private static void AddFakeViews(JsonTypeInfo typeInfo)
    {
        Type? fake = typeInfo.Type switch
        {
            var t when t == typeof(PlayerRoundView) => typeof(FakePlayerView),
            var t when t == typeof(DisplayRoundView) => typeof(FakeDisplayView),
            var t when t == typeof(GameMasterRoundView) => typeof(FakeGameMasterView),
            _ => null,
        };

        if (fake is not null)
        {
            typeInfo.PolymorphismOptions!.DerivedTypes.Add(new JsonDerivedType(fake, "fake"));
        }
    }
}
