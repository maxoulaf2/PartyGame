using System.Text.Json.Nodes;
using static PartyGame.TypeGen.Tests.Fixtures.PackSchemaFixtures;

namespace PartyGame.TypeGen.Tests;

public sealed class PackSchemaGeneratorTests
{
    private static readonly JsonObject _schema = JsonNode.Parse(PackSchemaGenerator.Generate(typeof(SampleDescriptor)))!.AsObject();

    private static JsonObject Properties => _schema["properties"]!.AsObject();

    [Fact]
    public void Generate_Root_DeclaresDialectFirstThenDescription()
    {
        Assert.Equal(["$schema", "description"], _schema.Select(p => p.Key).Take(2));
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", (string?)_schema["$schema"]);
        Assert.Equal("Un descripteur.", (string?)_schema["description"]);
    }

    [Fact]
    public void Generate_Objects_RefuseUnknownPropertiesAndListRequiredOnes()
    {
        Assert.False((bool)_schema["additionalProperties"]!);
        Assert.Equal(["title", "code", "version", "activities"], _schema["required"]!.AsArray().Select(n => (string?)n));
    }

    [Fact]
    public void Generate_StringLength_GivesMinAndMaxLength()
    {
        AssertConstraints(Properties["title"], ("minLength", 1), ("maxLength", 60));
        AssertConstraints(Properties["summary"], ("maxLength", 200));
        Assert.Null(Properties["summary"]!["minLength"]);
    }

    [Fact]
    public void Generate_LengthOnStringAndArray_GivesLengthOrItemBounds()
    {
        AssertConstraints(Properties["code"], ("minLength", 2), ("maxLength", 4));
        AssertConstraints(Properties["tags"], ("minItems", 2), ("maxItems", 4));
    }

    [Fact]
    public void Generate_MinAndMaxLengthOnArrays_GiveItemBounds()
    {
        AssertConstraints(Properties["activities"], ("minItems", 1));
        AssertConstraints(Properties["scores"], ("maxItems", 3));
    }

    [Fact]
    public void Generate_Range_GivesMinimumAndMaximum()
    {
        AssertConstraints(Properties["seconds"], ("minimum", 5), ("maximum", 120));
        Assert.Equal(0.5, (double)Properties["ratio"]!["minimum"]!);
        Assert.Equal(2.5, (double)Properties["ratio"]!["maximum"]!);
    }

    [Fact]
    public void Generate_RangeOfOneValue_GivesConstant()
    {
        AssertConstraints(Properties["version"], ("const", 1));
        Assert.Null(Properties["version"]!["minimum"]);
    }

    [Fact]
    public void Generate_Descriptions_ComeFromPropertyThenFromType()
    {
        Assert.Equal("Le titre.", (string?)Properties["title"]!["description"]);
        Assert.Equal("Des réglages.", (string?)Properties["settings"]!["description"]);
        Assert.Equal("Réglages propres à ce descripteur.", (string?)Properties["overrides"]!["description"]);
        Assert.Null(Properties["summary"]!["description"]);
    }

    [Fact]
    public void Generate_TypedIdentifier_GivesStringNullableWhenOptional()
    {
        Assert.Equal("""{"type":"string"}""", Properties["cover"]!.ToJsonString());
        Assert.Equal("""{"description":"Une image.","type":["string","null"]}""", Properties["picture"]!.ToJsonString());
    }

    [Fact]
    public void Generate_PolymorphicBase_OffersEachTypeWithItsDescription()
    {
        var items = Properties["activities"]!["items"]!;
        var variants = items["anyOf"]!.AsArray();

        Assert.Equal("Une activité.", (string?)items["description"]);
        Assert.Equal(["type"], items["required"]!.AsArray().Select(n => (string?)n));
        Assert.Equal(["quiz", "blindTest"], variants.Select(v => (string?)v!["properties"]!["type"]!["const"]));

        var quiz = variants[0]!;
        Assert.Equal("Un quiz.", (string?)quiz["description"]);
        Assert.Equal("Un quiz.", (string?)quiz["properties"]!["type"]!["description"]);
        Assert.False((bool)quiz["additionalProperties"]!);
        Assert.Null(variants[1]!["properties"]!["type"]!["description"]);
    }

    [Theory]
    [InlineData(typeof(WithRequired), "RequiredAttribute")]
    [InlineData(typeof(WithExclusiveRange), "RangeAttribute")]
    [InlineData(typeof(WithStringLengthOnNumber), "StringLengthAttribute")]
    public void Generate_UntranslatableAttribute_ThrowsNamingAttributeAndType(Type type, string attribute)
    {
        var exception = Assert.Throws<TypeGenException>(() => PackSchemaGenerator.Generate(type));

        Assert.Contains(attribute, exception.Message, StringComparison.Ordinal);
        Assert.Contains(type.FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_Output_IsStableIndentedWithLfAndReadableAccents()
    {
        var text = PackSchemaGenerator.Generate(typeof(SampleDescriptor));

        Assert.StartsWith("{\n  \"$schema\": ", text, StringComparison.Ordinal);
        Assert.EndsWith("}\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', text);
        Assert.Contains("Des réglages.", text, StringComparison.Ordinal);
        Assert.Equal(text, PackSchemaGenerator.Generate(typeof(SampleDescriptor)));
    }

    private static void AssertConstraints(JsonNode? node, params (string Keyword, int Value)[] expected)
    {
        Assert.NotNull(node);
        foreach (var (keyword, value) in expected)
        {
            Assert.Equal(value, (int?)node[keyword]);
        }
    }
}
