using System.Collections.Immutable;
using PartyGame.TypeGen.Tests.Fixtures;
using static PartyGame.TypeGen.Tests.Fixtures.SampleFixtures;
using static PartyGame.TypeGen.Tests.Fixtures.UnsupportedFixtures;

namespace PartyGame.TypeGen.Tests;

public sealed class ContractsGeneratorTests
{
    [Fact]
    public void Generate_Enum_WritesUnionOfMemberNames()
    {
        var files = ContractsGenerator.Generate([typeof(SampleColor)]);

        Assert.Equal("export type SampleColor = 'Red' | 'Green' | 'deep-blue';\n", Body(files, "SampleColor.ts"));
    }

    [Fact]
    public void Generate_TypedIds_WritesBrandedStrings()
    {
        var files = ContractsGenerator.Generate([typeof(SampleId), typeof(SampleCode)]);

        Assert.Equal("export type SampleId = string & { readonly __brand: 'SampleId' };\n", Body(files, "SampleId.ts"));
        Assert.Equal("export type SampleCode = string & { readonly __brand: 'SampleCode' };\n", Body(files, "SampleCode.ts"));
    }

    [Fact]
    public void Generate_Record_WritesInterfaceFollowingWireConventions()
    {
        var files = ContractsGenerator.Generate([typeof(SampleDto)]);

        Assert.Equal(
            """
            import type { SampleCode } from './SampleCode';
            import type { SampleColor } from './SampleColor';
            import type { SampleId } from './SampleId';
            import type { SampleItem } from './SampleItem';
            import type { SampleShape } from './SampleShape';

            export interface SampleDto {
                readonly id: SampleId;
                readonly code: SampleCode | null;
                readonly color: SampleColor;
                readonly accent: SampleColor | null;
                readonly nickname: string | null;
                readonly count: number;
                readonly ready: boolean;
                readonly createdAt: string;
                readonly items: readonly SampleItem[];
                readonly tags: readonly (string | null)[];
                readonly grid: readonly (readonly number[])[];
                readonly scores: Readonly<Record<string, number>>;
                readonly byName: Readonly<Record<string, SampleItem | null>>;
                readonly shape: SampleShape;
                readonly optionalShape: SampleShape | null;
                readonly custom_name: string;
            }

            """.ReplaceLineEndings("\n"),
            Body(files, "SampleDto.ts"));
        Assert.Equal(
            """
            export interface SampleItem {
                readonly label: string;
                readonly rank: number | null;
            }

            """.ReplaceLineEndings("\n"),
            Body(files, "SampleItem.ts"));
    }

    [Fact]
    public void Generate_PolymorphicBase_WritesDiscriminatedUnion()
    {
        var files = ContractsGenerator.Generate([typeof(SampleShape)]);

        Assert.Equal(
            """
            import type { SampleCircle } from './SampleCircle';
            import type { SampleSquare } from './SampleSquare';

            export type SampleShape = SampleCircle | SampleSquare;

            """.ReplaceLineEndings("\n"),
            Body(files, "SampleShape.ts"));
        Assert.Equal(
            """
            export interface SampleCircle {
                readonly type: 'circle';
                readonly name: string;
                readonly radius: number;
            }

            """.ReplaceLineEndings("\n"),
            Body(files, "SampleCircle.ts"));
        Assert.Contains("readonly type: 'square';", Body(files, "SampleSquare.ts"), StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_DerivedTypeAsRoot_GeneratesWholeUnion()
    {
        var files = ContractsGenerator.Generate([typeof(SampleCircle)]);

        Assert.Equal(
            ["SampleCircle.ts", "SampleShape.ts", "SampleSquare.ts", "index.ts"],
            files.Select(f => f.Path).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Generate_AnyType_EveryFileStartsWithGeneratedHeader()
    {
        var files = ContractsGenerator.Generate([typeof(SampleDto)]);

        Assert.All(files, file => Assert.StartsWith(TypeScriptWriter.Header, file.Content, StringComparison.Ordinal));
        Assert.Contains("Do not edit by hand", TypeScriptWriter.Header, StringComparison.Ordinal);
        Assert.Contains("npm run generate:contracts", TypeScriptWriter.Header, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_AnyType_IndexReExportsEveryDeclaration()
    {
        var files = ContractsGenerator.Generate([typeof(SampleShape), typeof(SampleColor)]);

        Assert.Equal(
            """
            export type * from './SampleCircle';
            export type * from './SampleColor';
            export type * from './SampleShape';
            export type * from './SampleSquare';

            """.ReplaceLineEndings("\n"),
            Body(files, TypeScriptWriter.IndexFile));
    }

    [Fact]
    public void Generate_NoType_WritesEmptyModuleIndex()
    {
        var files = ContractsGenerator.Generate(Array.Empty<Type>());

        Assert.Equal("export {};\n", Body(Assert.Single(files)));
    }

    [Fact]
    public void Generate_SameTypesInAnyOrder_ProducesSameFiles()
    {
        var forward = ContractsGenerator.Generate([typeof(SampleDto), typeof(SampleColor)]);
        var backward = ContractsGenerator.Generate([typeof(SampleColor), typeof(SampleDto)]);

        Assert.Equal(forward, backward);
    }

    [Theory]
    [InlineData(typeof(WithObject), "property 'WithObject.Payload'", "System.Object")]
    [InlineData(typeof(WithDateTime), "property 'WithDateTime.At'", "System.DateTime")]
    [InlineData(typeof(WithGeneric), "property 'WithGeneric.Box'", "generic type")]
    [InlineData(typeof(WithOptional), "property 'WithOptional.Note'", "WhenWritingNull")]
    [InlineData(typeof(WithDerivedType), "property 'WithDerivedType.Circle'", "reference 'SampleShape' instead")]
    [InlineData(typeof(WithFlags), "UnsupportedFixtures+Permissions", "[Flags]")]
    [InlineData(typeof(WithIntKeys), "property 'WithIntKeys.Labels'", "dictionary key type 'System.Int32'")]
    [InlineData(typeof(WithBytes), "property 'WithBytes.Data'", "base64")]
    [InlineData(typeof(WithInterface), "UnsupportedFixtures+IMarker", "interfaces and abstract types")]
    [InlineData(typeof(Undiscriminated), "UnsupportedFixtures+Undiscriminated", "'UndiscriminatedVariant' needs a type discriminator")]
    [InlineData(typeof(ConcreteBase), "UnsupportedFixtures+ConcreteBase", "must be abstract or an interface")]
    [InlineData(typeof(ClashingVariant), "property 'ClashingVariant.Type'", "JSON name 'type' is already used")]
    public void Generate_UnsupportedConstruct_FailsNamingTypeAndProperty(Type type, string expectedLocation, string expectedReason)
    {
        var exception = Assert.Throws<TypeGenException>(() => ContractsGenerator.Generate([type]));

        Assert.Contains(expectedLocation, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expectedReason, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_TwoTypesWithSameName_Fails()
    {
        var exception = Assert.Throws<TypeGenException>(
            () => ContractsGenerator.Generate([typeof(SampleDto), typeof(UnsupportedFixtures.SampleItem)]));

        Assert.Contains("would both generate 'SampleItem'", exception.Message, StringComparison.Ordinal);
    }

    private static string Body(ImmutableArray<GeneratedFile> files, string path) =>
        Body(Assert.Single(files, f => f.Path == path));

    private static string Body(GeneratedFile file)
    {
        Assert.StartsWith(TypeScriptWriter.Header, file.Content, StringComparison.Ordinal);
        return file.Content[TypeScriptWriter.Header.Length..];
    }
}
