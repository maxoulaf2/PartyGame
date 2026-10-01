using System.Text.Json.Serialization;

namespace PartyGame.TypeGen.Tests.Fixtures;

/// <summary>
/// Constructions the generator cannot translate faithfully, each of which must fail generation.
/// </summary>
public static class UnsupportedFixtures
{
    public sealed record WithObject(object Payload);

    public sealed record WithDateTime(DateTime At);

    public sealed record Box<T>(T Content);

    public sealed record WithGeneric(Box<int> Box);

    public sealed record WithOptional([property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Note);

    public sealed record WithDerivedType(SampleFixtures.SampleCircle Circle);

    [Flags]
    public enum Permissions
    {
        None = 0,
        Read = 1,
        Write = 2,
    }

    public sealed record WithFlags(Permissions Permissions);

    public sealed record WithIntKeys(IReadOnlyDictionary<int, string> Labels);

    public sealed record WithBytes(byte[] Data);

    public interface IMarker;

    public sealed record WithInterface(IMarker Marker);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(UndiscriminatedVariant))]
    public abstract record Undiscriminated;

    public sealed record UndiscriminatedVariant : Undiscriminated;

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(ConcreteBaseVariant), "variant")]
    public record ConcreteBase;

    public sealed record ConcreteBaseVariant : ConcreteBase;

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(ClashingVariant), "clash")]
    public abstract record ClashingBase;

    public sealed record ClashingVariant(string Type) : ClashingBase;

    /// <summary>Same name as <see cref="SampleFixtures.SampleItem"/>.</summary>
    public sealed record SampleItem(string Label);

    public interface IClientWithResult
    {
        Task<int> Ask();
    }

    public interface IClientWithOverloads
    {
        Task Notify();

        Task Notify(string text);
    }

    public interface IClientWithRef
    {
        Task Update(ref int value);
    }

    public interface IClientWithProperty
    {
        int Count { get; }
    }

    public interface IClientWithObject
    {
        Task Send(object payload);
    }
}
