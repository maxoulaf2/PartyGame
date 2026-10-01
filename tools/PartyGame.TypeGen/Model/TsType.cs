namespace PartyGame.TypeGen.Model;

/// <summary>
/// A TypeScript type expression, as used for a property or a collection element.
/// </summary>
internal abstract record TsType;

/// <summary><c>string</c>, <c>number</c> or <c>boolean</c>.</summary>
internal sealed record TsPrimitiveType(string Name) : TsType
{
    public static TsPrimitiveType String { get; } = new("string");

    public static TsPrimitiveType Number { get; } = new("number");

    public static TsPrimitiveType Boolean { get; } = new("boolean");
}

/// <summary><c>T | null</c>.</summary>
internal sealed record TsNullableType(TsType Inner) : TsType;

/// <summary><c>readonly T[]</c>.</summary>
internal sealed record TsArrayType(TsType Element) : TsType;

/// <summary><c>Readonly&lt;Record&lt;string, T&gt;&gt;</c>.</summary>
internal sealed record TsRecordType(TsType Value) : TsType;

/// <summary>A reference to a generated declaration.</summary>
internal sealed record TsReferenceType(string Name) : TsType;

/// <summary>A string or number literal, used for polymorphic discriminators.</summary>
internal sealed record TsLiteralType(object Value) : TsType;
