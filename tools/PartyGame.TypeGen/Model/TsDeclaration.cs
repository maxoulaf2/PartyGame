using System.Collections.Immutable;

namespace PartyGame.TypeGen.Model;

/// <summary>
/// A named TypeScript declaration generated from a C# type. Each one is written to its own file.
/// </summary>
internal abstract record TsDeclaration(string Name, Type Source);

/// <summary>An interface generated from a record or a class.</summary>
internal sealed record TsInterface(string Name, Type Source, ImmutableArray<TsProperty> Properties) : TsDeclaration(Name, Source);

/// <summary>A union of string literals generated from an enum.</summary>
internal sealed record TsStringUnion(string Name, Type Source, ImmutableArray<string> Values) : TsDeclaration(Name, Source);

/// <summary>A branded string generated from a typed identifier.</summary>
internal sealed record TsBrandedString(string Name, Type Source) : TsDeclaration(Name, Source);

/// <summary>A discriminated union generated from a polymorphic base type, one member per derived type.</summary>
internal sealed record TsUnion(string Name, Type Source, ImmutableArray<string> Members) : TsDeclaration(Name, Source);

/// <summary>
/// The messages a SignalR hub sends to its clients, generated from a hub client interface: one method per message,
/// named as its SignalR target.
/// </summary>
internal sealed record TsClientInterface(string Name, Type Source, ImmutableArray<TsMethod> Methods) : TsDeclaration(Name, Source);

/// <summary>A message of a hub client interface, with the arguments the server sends.</summary>
internal sealed record TsMethod(string Name, ImmutableArray<TsParameter> Parameters);

/// <summary>An argument of a hub client message.</summary>
internal sealed record TsParameter(string Name, TsType Type);

/// <summary>A property of a generated interface, always present in the JSON.</summary>
internal sealed record TsProperty(string Name, TsType Type);
