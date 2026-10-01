using System.Collections.Immutable;
using System.Text.Json;
using PartyGame.Contracts.Serialization;
using PartyGame.TypeGen.Model;
using static PartyGame.TypeGen.Tests.Fixtures.SampleFixtures;

namespace PartyGame.TypeGen.Tests;

/// <summary>
/// Checks the generated types against the JSON actually written with the wire conventions,
/// so that the generator and the serializer cannot drift apart.
/// </summary>
public sealed class WireShapeTests
{
    private static readonly ImmutableArray<TsDeclaration> _declarations = TypeModelBuilder.Build([typeof(SampleDto)]);

    public static TheoryData<string> Samples => [nameof(Filled), nameof(Empty)];

    [Theory]
    [MemberData(nameof(Samples))]
    public void Serialize_SampleDto_MatchesGeneratedTypes(string sample)
    {
        var dto = sample == nameof(Filled) ? Filled : Empty;
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, ContractJsonOptions.Default));

        var errors = new List<string>();
        Validate(new TsReferenceType(nameof(SampleDto)), json.RootElement, "$", errors);

        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Prepend(json.RootElement.GetRawText())));
    }

    private static SampleDto Filled { get; } = new(
        Id: new SampleId(Guid.NewGuid()),
        Code: new SampleCode("ABC"),
        Color: SampleColor.Blue,
        Accent: SampleColor.Green,
        Nickname: "Alice",
        Count: 3,
        Ready: true,
        CreatedAt: new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero),
        Items: [new SampleItem("first", 1), new SampleItem("second", null)],
        Tags: ["a", null],
        Grid: [[1, 2], []],
        Scores: ImmutableDictionary<SampleId, int>.Empty.Add(new SampleId(Guid.NewGuid()), 10),
        ByName: new Dictionary<string, SampleItem?> { ["first"] = new SampleItem("first", 1), ["none"] = null },
        Shape: new SampleCircle("circle", 1.5),
        OptionalShape: new SampleSquare("square", 2),
        Renamed: "renamed");

    private static SampleDto Empty { get; } = new(
        Id: new SampleId(Guid.Empty),
        Code: null,
        Color: SampleColor.Red,
        Accent: null,
        Nickname: null,
        Count: 0,
        Ready: false,
        CreatedAt: DateTimeOffset.UnixEpoch,
        Items: [],
        Tags: [],
        Grid: [],
        Scores: ImmutableDictionary<SampleId, int>.Empty,
        ByName: new Dictionary<string, SampleItem?>(),
        Shape: new SampleSquare("square", 0),
        OptionalShape: null,
        Renamed: string.Empty);

    private static void Validate(TsType type, JsonElement element, string path, List<string> errors)
    {
        switch (type)
        {
            case TsNullableType nullable:
                if (element.ValueKind != JsonValueKind.Null)
                {
                    Validate(nullable.Inner, element, path, errors);
                }

                break;

            case TsPrimitiveType primitive:
                var kindMatches = primitive.Name switch
                {
                    "string" => element.ValueKind == JsonValueKind.String,
                    "number" => element.ValueKind == JsonValueKind.Number,
                    "boolean" => element.ValueKind is JsonValueKind.True or JsonValueKind.False,
                    _ => false,
                };
                Expect(kindMatches, path, $"expected {primitive.Name}, got {element.ValueKind}", errors);
                break;

            case TsLiteralType literal:
                Expect(element.ToString() == Convert.ToString(literal.Value, System.Globalization.CultureInfo.InvariantCulture), path, $"expected literal {literal.Value}, got {element}", errors);
                break;

            case TsArrayType array:
                if (Expect(element.ValueKind == JsonValueKind.Array, path, $"expected an array, got {element.ValueKind}", errors))
                {
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        Validate(array.Element, item, $"{path}[{index++}]", errors);
                    }
                }

                break;

            case TsRecordType record:
                if (Expect(element.ValueKind == JsonValueKind.Object, path, $"expected an object, got {element.ValueKind}", errors))
                {
                    foreach (var entry in element.EnumerateObject())
                    {
                        Validate(record.Value, entry.Value, $"{path}.{entry.Name}", errors);
                    }
                }

                break;

            case TsReferenceType reference:
                ValidateDeclaration(_declarations.Single(d => d.Name == reference.Name), element, path, errors);
                break;

            default:
                errors.Add($"{path}: unexpected type {type}");
                break;
        }
    }

    private static void ValidateDeclaration(TsDeclaration declaration, JsonElement element, string path, List<string> errors)
    {
        switch (declaration)
        {
            case TsBrandedString:
                Expect(element.ValueKind == JsonValueKind.String, path, $"expected a {declaration.Name} string, got {element.ValueKind}", errors);
                break;

            case TsStringUnion union:
                Expect(
                    element.ValueKind == JsonValueKind.String && union.Values.Contains(element.GetString()!),
                    path,
                    $"expected one of {string.Join(", ", union.Values)}, got {element}",
                    errors);
                break;

            case TsUnion union:
                // The member is the one whose discriminator literal matches.
                var member = union.Members
                    .Select(name => (TsInterface)_declarations.Single(d => d.Name == name))
                    .SingleOrDefault(m => m.Properties[0] is { Type: TsLiteralType literal } discriminator
                        && element.TryGetProperty(discriminator.Name, out var value)
                        && value.ToString() == literal.Value.ToString());
                if (Expect(member is not null, path, $"no member of {union.Name} matches {element}", errors))
                {
                    ValidateDeclaration(member!, element, path, errors);
                }

                break;

            case TsInterface tsInterface:
                if (!Expect(element.ValueKind == JsonValueKind.Object, path, $"expected {tsInterface.Name}, got {element.ValueKind}", errors))
                {
                    break;
                }

                var actual = element.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
                var expected = tsInterface.Properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
                Expect(actual.SetEquals(expected), path, $"properties [{string.Join(", ", actual)}] differ from {tsInterface.Name} [{string.Join(", ", expected)}]", errors);

                foreach (var property in tsInterface.Properties.Where(p => actual.Contains(p.Name)))
                {
                    Validate(property.Type, element.GetProperty(property.Name), $"{path}.{property.Name}", errors);
                }

                break;
        }
    }

    private static bool Expect(bool condition, string path, string message, List<string> errors)
    {
        if (!condition)
        {
            errors.Add($"{path}: {message}");
        }

        return condition;
    }
}
